using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Esdb.Tcp {
    class StreamMetaCache {
        readonly ConcurrentDictionary<string, StreamSize> _streamsSize = new();

        readonly ConcurrentDictionary<string, StreamMeta> _streamsMeta = new();

        public Task<StreamMeta> GetOrAddStreamMeta(string stream, Func<string, Task<StreamMeta>> getMeta)
            => _streamsMeta.GetOrAddAsync(stream, () => getMeta(stream));

        public void UpdateStreamMeta(string stream, StreamMetadata streamMetadata, long version) {
            var isDeleted = IsStreamDeleted(streamMetadata);

            if (!_streamsMeta.TryGetValue(stream, out var meta)) {
                _streamsMeta[stream] =
                    new StreamMeta(
                        isDeleted,
                        isDeleted ? version : 0,
                        streamMetadata.MaxAge,
                        streamMetadata.MaxCount,
                        version
                    );
                return;
            }

            if (meta.Version > version) return;

            if (streamMetadata.MaxAge.HasValue)
                meta = meta with {MaxAge = streamMetadata.MaxAge};

            if (streamMetadata.MaxCount.HasValue)
                meta = meta with {MaxCount = streamMetadata.MaxCount};

            if (isDeleted)
                meta = meta with {IsDeleted = true};

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

    record StreamMeta(bool IsDeleted, long DeletedAt, TimeSpan? MaxAge, long? MaxCount, long Version);
}