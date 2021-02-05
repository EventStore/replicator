// ReSharper disable SuggestBaseTypeForParameter

using System;

namespace EventStore.Replicator.Shared.Contracts {
    public abstract record BaseOriginalEvent(
        DateTimeOffset Created,
        EventDetails   EventDetails,
        Position       Position,
        long           SequenceNumber
    );

    public record OriginalEvent(
        DateTimeOffset Created,
        EventDetails   EventDetails,
        byte[]         Data,
        byte[]?        Metadata,
        Position       Position,
        long           SequenceNumber
    ) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber);

    public record StreamMetadataOriginalEvent(
        DateTimeOffset Created,
        EventDetails   EventDetails,
        StreamMetadata Data,
        Position       Position,
        long           SequenceNumber
    ) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber);

    public record StreamDeletedOriginalEvent(
        DateTimeOffset Created,
        EventDetails   EventDetails,
        Position       Position,
        long           SequenceNumber
    ) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber);
}