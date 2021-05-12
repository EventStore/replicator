using System;
using System.Threading;
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

        public ReaderPipe(
            IEventReader reader, ICheckpointStore checkpointStore, Func<PrepareContext, ValueTask> send
        ) {
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
                    var start = await checkpointStore.LoadCheckpoint(ctx.CancellationToken).ConfigureAwait(false);
                    log.Info("Reading from {Position}", start);

                    await reader.ReadEvents(
                        start,
                        async read => {
                            ReplicationMetrics.ReadingPosition.Set(read.Position.EventPosition);

                            await send(new PrepareContext(read, ctx.CancellationToken)).ConfigureAwait(false);
                        },
                        ctx.CancellationToken
                    ).ConfigureAwait(false);
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