using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Shared; 

public interface IEventWriter {
    Task Start();
    Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken);
}