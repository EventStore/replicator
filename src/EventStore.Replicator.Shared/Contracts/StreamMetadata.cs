namespace EventStore.Replicator.Shared.Contracts;

public record StreamMetadata(
    int?       MaxCount,
    TimeSpan?  MaxAge,
    long?      TruncateBefore,
    TimeSpan?  CacheControl,
    StreamAcl? StreamAcl
);