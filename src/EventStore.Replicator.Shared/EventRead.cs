using System;

namespace EventStore.Replicator.Shared {
    public class EventRead {
        public DateTime Created       { get; set; }
        public byte[]   Data          { get; set; }
        public byte[]   Metadata      { get; set; }
        public Guid     EventId       { get; set; }
        public string   EventType     { get; set; }
        public bool     IsJson        { get; set; }
        public string   EventStreamId { get; set; }
        public Position Position      { get; set; }
    }
}
