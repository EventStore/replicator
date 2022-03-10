// ReSharper disable SuggestBaseTypeForParameter

namespace EventStore.Replicator.Shared.Contracts; 

public abstract record BaseOriginalEvent(
    DateTimeOffset  Created,
    EventDetails    EventDetails,
    Position        Position,
    long            SequenceNumber,
    TracingMetadata TracingMetadata
);

public record OriginalEvent(
    DateTimeOffset  Created,
    EventDetails    EventDetails,
    byte[]          Data,
    byte[]?         Metadata,
    Position        Position,
    long            SequenceNumber,
    TracingMetadata TracingMetadata
) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber, TracingMetadata);

public record StreamMetadataOriginalEvent(
    DateTimeOffset  Created,
    EventDetails    EventDetails,
    StreamMetadata  Data,
    Position        Position,
    long            SequenceNumber,
    TracingMetadata TracingMetadata
) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber, TracingMetadata);

public record StreamDeletedOriginalEvent(
    DateTimeOffset  Created,
    EventDetails    EventDetails,
    Position        Position,
    long            SequenceNumber,
    TracingMetadata TracingMetadata
) : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber, TracingMetadata);

public record IgnoredOriginalEvent(
        DateTimeOffset  Created,
        EventDetails    EventDetails,
        Position        Position,
        long            SequenceNumber,
        TracingMetadata TracingMetadata
    )
    : BaseOriginalEvent(Created, EventDetails, Position, SequenceNumber, TracingMetadata);