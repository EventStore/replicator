namespace EventStore.Replicator.Shared; 

public interface ICheckpointStore {
    ValueTask<bool> HasStoredCheckpoint(CancellationToken cancellationToken);
    
    ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken);

    ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken);

    ValueTask Flush(CancellationToken cancellationToken);
}