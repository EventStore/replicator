using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator; 

public interface IEventDetailsContext {
    public EventDetails EventDetails { get; }
}