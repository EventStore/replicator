using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator;

public class ChaserCheckpointSeeder : ICheckpointSeeder {
    static readonly ILog             Log = LogProvider.GetCurrentClassLogger();
    
    readonly        string           _filePath;
    readonly        ICheckpointStore _checkpointStore;
    
    public ChaserCheckpointSeeder(string filePath, ICheckpointStore checkpointStore) {
        _filePath        = filePath;
        _checkpointStore = checkpointStore;
    }

    public async ValueTask Seed(CancellationToken cancellationToken) {
        if (!await _checkpointStore.HasStoredCheckpoint(cancellationToken)) {
            if (File.Exists(_filePath)) {
                using var fileStream = new FileStream(
                    _filePath,
                    FileMode.Open,
                    FileAccess.Read,
                    FileShare.ReadWrite
                );

                if (fileStream.Length == 8) {
                    using var reader   = new BinaryReader(fileStream);
                    var       position = reader.ReadInt64();
                    await _checkpointStore.StoreCheckpoint(new Position(0L, (ulong)position), cancellationToken);
                }
                else {
                    Log.Warn(
                        "Seeding failed because the file at {FilePath} does not appear to be an 8-byte position file",
                        _filePath
                    );
                }
            }
            else {
                Log.Warn("Seeding failed because the file at {FilePath} does not exist", _filePath);
            }
        }
        else {
            Log.Info("Checkpoint already present in store, skipping seeding");
        }
    }
}