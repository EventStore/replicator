// ReSharper disable SuggestBaseTypeForParameter

namespace EventStore.Replicator.Shared.Contracts {
    public abstract record BaseProposedEvent(
        EventDetails EventDetails,
        Position     SourcePosition,
        long         SequenceNumber
    );

    public record ProposedEvent(
        EventDetails EventDetails,
        byte[]       Data,
        byte[]?      Metadata,
        Position     SourcePosition,
        long         SequenceNumber
    ) : BaseProposedEvent(EventDetails, SourcePosition, SequenceNumber);

    public record ProposedMetaEvent(
        EventDetails   EventDetails,
        StreamMetadata Data,
        Position       SourcePosition,
        long           SequenceNumber
    ) : BaseProposedEvent(EventDetails, SourcePosition, SequenceNumber);

    public record ProposedDeleteStream(EventDetails EventDetails, Position SourcePosition, long SequenceNumber)
        : BaseProposedEvent(EventDetails, SourcePosition, SequenceNumber);
}