using System;
using System.Threading.Tasks;
using EventStore.Replicator.Observers;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using GreenPipes;

namespace EventStore.Replicator.Sink {
    public class SinkPipe {
        readonly IPipe<SinkContext> _pipe;

        public SinkPipe(IEventWriter writer, ICheckpointStore checkpointStore)
            => _pipe = Pipe.New<SinkContext>(
                cfg => {
                    cfg.UseRetry(
                        r => {
                            r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
                            r.ConnectRetryObserver(new LoggingRetryObserver());
                        }
                    );
                    cfg.UseConcurrencyLimit(1);
                    // cfg.UsePartitioner(2, x => x.ProposedEvent.EventDetails.Stream);

                    cfg.UseLog();

                    cfg.UseEventWriter(writer);
                    cfg.UseCheckpointStore(checkpointStore);
                    cfg.UseExecute(ctx => Counters.SetLastProcessed(ctx.ProposedEvent.SourcePosition.EventPosition));
                }
            );

        public Task Send(SinkContext context) => _pipe.Send(context);
    }

    static class SinkPipelineExtensions {
        public static void UseEventWriter(this IPipeConfigurator<SinkContext> cfg, IEventWriter writer)
            => cfg.UseExecuteAsync(
                ctx => writer.WriteEvent(ctx.ProposedEvent, ctx.CancellationToken)
            );

        public static void UseCheckpointStore(
            this IPipeConfigurator<SinkContext> cfg, ICheckpointStore checkpointStore
        )
            => cfg.UseExecuteAsync(
                async ctx => await checkpointStore.StoreCheckpoint(
                    ctx.ProposedEvent.SourcePosition,
                    ctx.CancellationToken
                )
            );
    }
}