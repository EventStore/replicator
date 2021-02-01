// ReSharper disable SuggestBaseTypeForParameter

namespace EventStore.Replicator.Shared.Contracts {
    public record ProposedEvent(
        EventDetails EventDetails,
        byte[]       Data,
        byte[]       Metadata,
        Position     SourcePosition,
        long         SequenceNumber
    );
}
