using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Sink {
    public class SinkContext : BasePipeContext, PipeContext {
        public SinkContext(
            BaseProposedEvent proposedEvent, TracingMetadata tracingMetadata, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            ProposedEvent = proposedEvent;
            TracingMetadata = tracingMetadata;
        }

        public BaseProposedEvent ProposedEvent { get; }

        public TracingMetadata TracingMetadata { get; }
    }
}