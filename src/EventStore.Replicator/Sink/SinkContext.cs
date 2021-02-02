using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Sink {
    public class SinkContext : BasePipeContext, PipeContext {
        public SinkContext(
            ProposedEvent proposedEvent, Metadata metadata, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            ProposedEvent = proposedEvent;
            Metadata      = metadata;
        }

        public ProposedEvent ProposedEvent { get; }
        public Metadata      Metadata      { get; }
    }

}
