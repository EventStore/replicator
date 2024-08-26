using EventStore.Replicator.Shared;

namespace EventStore.Replicator;

public class NoCheckpointSeeder : ICheckpointSeeder {
    public ValueTask Seed(CancellationToken cancellationToken) => ValueTask.CompletedTask;
}