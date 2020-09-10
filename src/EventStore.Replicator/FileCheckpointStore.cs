using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;

namespace EventStore.Replicator {
    public class FileCheckpointStore : ICheckpointStore {
        const string FileName = "checkpoint";

        public async ValueTask<Position> LoadCheckpoint() {
            var content = await File.ReadAllTextAsync(FileName);
            var numbers = content.Split(',').Select(x => Convert.ToInt64(x)).ToArray();
            return new Position {EventNumber = numbers[0], CommitPosition = numbers[1], PreparePosition = numbers[2]};
        }

        public async ValueTask StoreCheckpoint(Position position)
            => await File.WriteAllTextAsync(FileName, $"{position.EventNumber},{position.CommitPosition},{position.PreparePosition}");
    }
}
