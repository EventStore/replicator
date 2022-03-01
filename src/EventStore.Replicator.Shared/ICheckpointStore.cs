namespace EventStore.Replicator.Shared; 

public interface ICheckpointStore {
    ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken);

    ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken);

    ValueTask Flush(CancellationToken cancellationToken);
}