using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Sink {
    public record SinkPipelineOptions(
        IEventWriter Writer,
        int          ConcurrencyLevel = 1,
        int          PartitionCount   = 1
    );
}
