using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Sink; 

public record SinkPipeOptions(
    IEventWriter Writer,
    int          PartitionCount = 1,
    int          BufferSize     = 1000,
    string?      Partitioner    = null
);