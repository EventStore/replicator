namespace EventStore.Replicator.Shared {
    public record Position(long EventNumber, long EventPosition) {
        public static Position Start = new Position(0, 0);
    }
}
