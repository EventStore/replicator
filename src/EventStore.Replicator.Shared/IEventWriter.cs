using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Shared; 

public interface IEventWriter {
    string Protocol { get; }
        
    Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken);
}