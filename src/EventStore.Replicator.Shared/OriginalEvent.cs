using System;

namespace EventStore.Replicator.Shared {
    public record OriginalEvent {
        public DateTime Created        { get; init; }
        public byte[]   Data           { get; init; }
        public byte[]   Metadata       { get; init; }
        public Guid     EventId        { get; init; }
        public string   EventType      { get; init; }
        public bool     IsJson         { get; init; }
        public string   EventStreamId  { get; init; }
        public Position Position       { get; init; }
        public long     SequenceNumber { get; set; }
    }
}
