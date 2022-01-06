namespace EventStore.Replicator.Shared; 

public record Position(long EventNumber, ulong EventPosition) {
    public static readonly Position Start = new(0, 0);
}