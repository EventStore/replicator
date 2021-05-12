using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator {
    public class FileCheckpointStore : ICheckpointStore {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly string _fileName;
        readonly int _checkpointAfter;

        public FileCheckpointStore(string filePath, int checkpointAfter) {
            _fileName        = filePath;
            _checkpointAfter = checkpointAfter;

            try {
                if (File.Exists(filePath))
                    return;

                File.AppendAllText(filePath, "test");
                File.Delete(filePath);
            }
            catch (Exception e) {
                Log.Fatal(e, "Unable to write to {File}", filePath);
                throw;
            }
        }

        public async ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken) {
            if (_lastPosition != null) {
                Log.Info("Starting from a previously known checkpoint {LastKnown}", _lastPosition);
                return _lastPosition;
            }

            if (!File.Exists(_fileName)) {
                Log.Info("No checkpoint file found, starting from the beginning");
                return Position.Start;
            }

            var content = await File.ReadAllTextAsync(_fileName, cancellationToken).ConfigureAwait(false);
            var numbers = content.Split(',').Select(x => Convert.ToInt64(x)).ToArray();
            
            Log.Info("Loaded the checkpoint from file: {Checkpoint}", numbers[1]);
            
            return new Position(numbers[0], (ulong) numbers[1]);
        }

        int _counter;
        Position? _lastPosition;

        public async ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken) {
            _lastPosition = position;
            
            Interlocked.Increment(ref _counter);
            if (_counter < _checkpointAfter) return;

            await Flush(cancellationToken).ConfigureAwait(false);

            Interlocked.Exchange(ref _counter, 0);
        }

        public async ValueTask Flush(CancellationToken cancellationToken) {
            if (_lastPosition == null) return;
            
            await File.WriteAllTextAsync(
                _fileName,
                $"{_lastPosition.EventNumber},{_lastPosition.EventPosition}",
                cancellationToken
            ).ConfigureAwait(false);
        }
    }
}