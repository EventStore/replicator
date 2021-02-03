using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using GreenPipes;

namespace EventStore.Replicator.Read {
    public class ReaderPipe {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IPipe<ReaderContext> _pipe;

        public ReaderPipe(
            IEventReader                  reader,
            Position                      start,
            ChannelWriter<PrepareContext> channel
        )
            => _pipe = Pipe.New<ReaderContext>(
                cfg => {
                    cfg.UseInlineFilter(
                        async (ctx, next) => {
                            try {
                                while (!ctx.CancellationToken.IsCancellationRequested) {
                                    await foreach (var read in reader.ReadEvents(
                                        start,
                                        ctx.CancellationToken
                                    )) {
                                        using var activity = new Activity("read").Start();

                                        var meta = new Metadata(activity.TraceId, activity.SpanId);

                                        await channel.WriteAsync(
                                            new PrepareContext(read, meta, ctx.CancellationToken),
                                            ctx.CancellationToken
                                        );
                                    }
                                }
                            }
                            catch (OperationCanceledException) {
                                // it's ok
                            }
                            finally {
                                Log.Info("Reader stopped");
                            }
                        }
                    );

                    cfg.UseRetry(
                        retry => retry.Incremental(
                            100,
                            TimeSpan.Zero,
                            TimeSpan.FromMilliseconds(100)
                        )
                    );
                }
            );

        public Task Start(CancellationToken stoppingToken)
            => _pipe.Send(new ReaderContext(stoppingToken));
    }

    public class ReaderContext : BasePipeContext, PipeContext {
        public ReaderContext(CancellationToken cancellationToken) : base(cancellationToken) { }
    }
}
