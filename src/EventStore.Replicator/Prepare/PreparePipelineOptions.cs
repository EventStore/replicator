using System.Collections.Generic;
using EventStore.Replicator.Shared.Pipeline;

namespace EventStore.Replicator.Prepare {
    public record PreparePipelineOptions(
        IEnumerable<FilterEvent> Filters,
        TransformEvent           Transform,
        int                      TransformConcurrencyLevel
    );
}
