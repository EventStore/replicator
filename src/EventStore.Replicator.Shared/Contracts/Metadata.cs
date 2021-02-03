using System.Diagnostics;

namespace EventStore.Replicator.Shared.Contracts {
    public record Metadata(ActivityTraceId TraceId, ActivitySpanId SpanId);
}
