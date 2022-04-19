using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Sink;

public class SinkContext : BasePipeContext, PipeContext, IEventDetailsContext {
    public SinkContext(BaseProposedEvent proposedEvent, CancellationToken cancellationToken)
        : base(cancellationToken)
        => ProposedEvent = proposedEvent;

    public BaseProposedEvent ProposedEvent { get; }
    public EventDetails      EventDetails  => ProposedEvent.EventDetails;
}