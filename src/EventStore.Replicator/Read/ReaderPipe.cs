using EventStore.Replicator.Observers;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using GreenPipes;

namespace EventStore.Replicator.Read;

public class ReaderPipe {
    readonly IPipe<ReaderContext> _pipe;

    public ReaderPipe(IEventReader reader, ICheckpointStore checkpointStore, Func<PrepareContext, ValueTask> send) {
        var log = LogProvider.GetCurrentClassLogger();

        _pipe = Pipe.New<ReaderContext>(
            cfg => {
                cfg.UseConcurrencyLimit(1);

                cfg.UseRetry(
                    retry => {
                        retry.Incremental(
                            50,
                            TimeSpan.Zero,
                            TimeSpan.FromMilliseconds(10)
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
            catch (Exception e) {
                log.Error(e, "Reader error");
            }
            finally {
                log.Info("Reader stopped");
            }
        }
    }

    public async Task Start(CancellationToken stoppingToken) {
        try {
            await _pipe.Send(new ReaderContext(stoppingToken));
        }
        catch (Exception e) {
            Console.WriteLine(e);
            throw;
        }
    }
}

public class ReaderContext : BasePipeContext, PipeContext {
    public ReaderContext(CancellationToken cancellationToken) : base(cancellationToken) { }
}