using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;

namespace EventStore.Replicator {
    public class FileCheckpointStore : ICheckpointStore {
        const string FileName = "checkpoint";

        public async ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken) {
            var content = await File.ReadAllTextAsync(FileName, cancellationToken);
            var numbers = content.Split(',').Select(x => Convert.ToInt64(x)).ToArray();
            return new Position(numbers[0], numbers[1]);
        }

        public async ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken)
            => await File.WriteAllTextAsync(FileName, $"{position.EventNumber},{position.EventPosition}", cancellationToken);
    }
}
