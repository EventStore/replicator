namespace EventStore.Replicator.Shared;

public interface ICheckpointSeeder {
    ValueTask Seed(CancellationToken cancellationToken);
}