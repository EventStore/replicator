namespace EventStore.Replicator.Shared {
    public class Position {
        public long EventNumber     { get; set; }
        public long CommitPosition  { get; set; }
        public long PreparePosition { get; set; }
        public override string ToString() => $"Event number: {EventNumber}, Prepare: {PreparePosition}, Commit: {CommitPosition}";
    }
}
