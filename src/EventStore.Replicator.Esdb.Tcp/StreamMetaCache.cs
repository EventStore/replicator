using System.Collections.Concurrent;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Extensions;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Esdb.Tcp; 

class StreamMetaCache {
    static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
    readonly ConcurrentDictionary<string, StreamSize> _streamsSize = new();
    readonly ConcurrentDictionary<string, StreamMeta> _streamsMeta = new();

    public async Task<StreamMeta?> GetOrAddStreamMeta(string stream, Func<string, Task<StreamMeta>> getMeta) {
        try {
            var meta = await _streamsMeta.GetOrAddAsync(stream, () => getMeta(stream));
            return meta;
        }
        catch (Exception e) {
            Log.Warn(e, "Unable to read metadata for stream {Stream}", stream);
            return null;
        }
    }

    public void UpdateStreamMeta(string stream, StreamMetadata streamMetadata, long version, DateTime created) {
        var isDeleted = IsStreamDeleted(streamMetadata);

        if (!_streamsMeta.TryGetValue(stream, out var meta)) {
            _streamsMeta[stream] =
                new StreamMeta(
                    isDeleted,
                    isDeleted ? created : DateTime.MaxValue,
                    streamMetadata.MaxAge,
                    streamMetadata.MaxCount,
                    streamMetadata.TruncateBefore,
                    version
                );
            return;
        }

        if (meta.Version > version) return;

        if (streamMetadata.MaxAge.HasValue)
            meta = meta with {MaxAge = streamMetadata.MaxAge};

        if (streamMetadata.MaxCount.HasValue)
            meta = meta with {MaxCount = streamMetadata.MaxCount};

        if (streamMetadata.TruncateBefore.HasValue)
            meta = meta with {TruncateBefore = streamMetadata.TruncateBefore};

        if (isDeleted)
            meta = meta with {IsDeleted = true, DeletedAt = created};

        _streamsMeta[stream] = meta;
    }

    public Task<StreamSize> GetOrAddStreamSize(string stream, Func<string, Task<StreamSize>> getSize)
        => _streamsSize.GetOrAddAsync(stream, () => getSize(stream));

    public void UpdateStreamLastEventNumber(string stream, long lastEventNumber) {
        if (!_streamsSize.TryGetValue(stream, out var size) || size.LastEventNumber < lastEventNumber) {
            _streamsSize[stream] = new StreamSize(lastEventNumber);
        }
    }

    static bool IsStreamDeleted(StreamMetadata meta) => meta.TruncateBefore == long.MaxValue;
}

record StreamSize(long LastEventNumber);

record StreamMeta(bool IsDeleted, DateTime DeletedAt, TimeSpan? MaxAge, long? MaxCount, long? TruncateBefore, long Version);