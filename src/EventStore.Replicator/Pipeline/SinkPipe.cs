using System.Threading;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Pipeline {
    public class SinkContext : BasePipeContext, PipeContext {
        public SinkContext(ProposedEvent proposedEvent, CancellationToken cancellationToken)
            : base(cancellationToken)
            => ProposedEvent = proposedEvent;

        public ProposedEvent ProposedEvent { get; }
    }

    public static class SinkPipelineExtensions {
        public static void UseEventWriter(
            this IPipeConfigurator<SinkContext> cfg, IEventWriter writer
        )
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
