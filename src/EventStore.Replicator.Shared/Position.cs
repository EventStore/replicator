namespace EventStore.Replicator.Shared; 

public record Position(long EventNumber, ulong EventPosition) {
    public static Position Start = new(0, 0);
}