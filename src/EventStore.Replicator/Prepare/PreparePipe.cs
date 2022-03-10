using EventStore.Replicator.Observers;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;
using GreenPipes;

namespace EventStore.Replicator.Prepare; 

public class PreparePipe {
    readonly IPipe<PrepareContext> _pipe;

    public PreparePipe(
        FilterEvent? filter, TransformEvent? transform, Func<SinkContext, ValueTask> send
    )
        => _pipe = Pipe.New<PrepareContext>(
            cfg => {
                cfg.UseRetry(
                    r => {
                        r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(10));
                        r.ConnectRetryObserver(new LoggingRetryObserver());
                    }
                );
                cfg.UseLog();
                cfg.UseConcurrencyLimit(10);

                cfg.UseEventFilter(filter ?? Filters.EmptyFilter);

                cfg.UseEventTransform(transform ?? Transforms.DefaultWithExtraMeta);

                cfg.UseExecuteAsync(
                    async ctx => {
                        var proposedEvent = ctx.GetPayload<BaseProposedEvent>();

                        try {
                            await send(
                                new SinkContext(
                                    proposedEvent,
                                    CancellationToken.None
                                )
                            ).ConfigureAwait(false);
                        }
                        catch (OperationCanceledException) { }
                    }
                );
            }
        );

    public Task Send(PrepareContext context) => _pipe.Send(context);
}