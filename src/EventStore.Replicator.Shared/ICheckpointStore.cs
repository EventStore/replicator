using System.Threading.Tasks;

namespace EventStore.Replicator.Shared {
    public interface ICheckpointStore {
        ValueTask<Position> LoadCheckpoint();
        ValueTask StoreCheckpoint(Position position);
    }
}
