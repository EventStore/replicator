using EventStore.Replicator.Shared.Pipeline;

namespace EventStore.Replicator.Prepare {
    public record PreparePipelineOptions(
        FilterEvent?    Filter,
        TransformEvent? Transform,
        int             TransformConcurrencyLevel = 1,
        int             BufferSize                = 1000
    );
}