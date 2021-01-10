using System;

namespace EventStore.Replicator.Shared {
    public record ProposedEvent {
        public string   Stream         { get; init; }
        public Guid     EventId        { get; init; }
        public string   EventType      { get; init; }
        public bool     IsJson         { get; init; }
        public byte[]   Data           { get; init; }
        public byte[]   Metadata       { get; init; }
        public Position SourcePosition { get; init; }
        public long     SequenceNumber { get; set; }
    }
}
