using System.Diagnostics;

namespace EventStore.Replicator.Shared.Contracts {
    public record TracingMetadata(ActivityTraceId TraceId, ActivitySpanId SpanId);
}
