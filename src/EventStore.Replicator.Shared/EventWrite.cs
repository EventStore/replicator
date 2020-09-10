using System;

namespace EventStore.Replicator.Shared {
    public class EventWrite {
        public string Stream          { get; set; }
        public Guid   EventId         { get; set; }
        public string EventType       { get; set; }
        public bool   IsJson          { get; set; }
        public byte[] Data            { get; set; }
        public byte[] Metadata        { get; set; }
        public long   ExpectedVersion { get; set; }
    }
}
