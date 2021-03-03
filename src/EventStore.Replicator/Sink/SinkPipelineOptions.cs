using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Sink {
    public record SinkPipeOptions(
        IEventWriter Writer,
        int          ConcurrencyLevel = 1,
        int          PartitionCount   = 1
    );
}
