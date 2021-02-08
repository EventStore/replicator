using System;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class PreparePipe {
        readonly IPipe<PrepareContext> _pipe;

        public PreparePipe(
            FilterEvent? filter, TransformEvent? transform, Func<SinkContext, ValueTask> send
        )
            => _pipe = Pipe.New<PrepareContext>(
                cfg => {
                    cfg.UseRetry(
                        r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
                    );
                    cfg.UseConcurrencyLimit(10);

                    cfg.UseEventFilter(filter ?? Filters.EmptyFilter);

                    cfg.UseEventTransform(transform ?? Transforms.Default);

                    cfg.UseExecuteAsync(
                        async ctx => await send(
                            new SinkContext(
                                ctx.GetPayload<ProposedEvent>(),
                                ctx.TracingMetadata,
                                ctx.CancellationToken
                            )
                        )
                    );
                }
            );

        public Task Send(PrepareContext context) => _pipe.Send(context);
    }
}
