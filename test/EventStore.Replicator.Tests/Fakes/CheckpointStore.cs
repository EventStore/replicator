using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Tests.Fakes; 

public class CheckpointStore : ICheckpointStore {
    public ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken)
        => ValueTask.FromResult(Position.Start);

    public ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken)
        => ValueTask.CompletedTask;

    public ValueTask Flush(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}