using System;

namespace EventStore.Replicator.Shared.Contracts {
    public record StreamMetadata(
        long? MaxCount, TimeSpan? MaxAge, long? TruncateBefore, TimeSpan? CacheControl, StreamAcl StreamAcl
    );
}
