// ReSharper disable SuggestBaseTypeForParameter

using System;

namespace EventStore.Replicator.Shared.Contracts {
    public record OriginalEvent(
        DateTimeOffset Created,
        EventDetails   EventDetails,
        byte[]         Data,
        byte[]         Metadata,
        Position       Position,
        long           SequenceNumber
    );
}
