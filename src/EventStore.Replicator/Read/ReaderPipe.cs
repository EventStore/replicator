using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Observers;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using GreenPipes;

namespace EventStore.Replicator.Read {
    public class ReaderPipe {
        readonly IPipe<ReaderContext> _pipe;

        public ReaderPipe(IEventReader reader, ICheckpointStore checkpointStore, ChannelWriter<PrepareContext> channel) {
            ILog log = LogProvider.GetCurrentClassLogger();

            _pipe = Pipe.New<ReaderContext>(
                cfg => {
                    cfg.UseConcurrencyLimit(1);

                    cfg.UseRetry(
                        retry => {
                            retry.Incremental(
                                100,
                                TimeSpan.Zero,
                                TimeSpan.FromMilliseconds(100)
                            );
                            retry.ConnectRetryObserver(new LoggingRetryObserver());
                        }
                    );
                    cfg.UseLog();

                    cfg.UseExecuteAsync(Reader);
                }
            );

            async Task Reader(ReaderContext ctx) {
                try {
                    var start = await checkpointStore.LoadCheckpoint(ctx.CancellationToken);
                    log.Info("Reading from {Position}", start);
                    
                    await foreach (var read in reader.ReadEvents(
                        start,
                        ctx.CancellationToken
                    )) {
                        Counters.SetLastRead(read.Position.EventPosition);

                        await channel.WriteAsync(
                            new PrepareContext(read, ctx.CancellationToken),
                            ctx.CancellationToken
                        );
                    }
                }
                catch (OperationCanceledException) {
                    // it's ok
                }
                finally {
                    log.Info("Reader stopped");
                }
            }
        }

        public Task Start(CancellationToken stoppingToken)
            => _pipe.Send(new ReaderContext(stoppingToken));
    }

    public class ReaderContext : BasePipeContext, PipeContext {
        public ReaderContext(CancellationToken cancellationToken) : base(cancellationToken) { }
    }
}