namespace EventStore.Replicator.Shared {
    public record Position(long EventNumber, long EventPosition);
}
