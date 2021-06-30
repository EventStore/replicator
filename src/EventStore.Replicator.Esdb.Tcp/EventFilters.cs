using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Esdb.Tcp {
    class ScavengedEventsFilter {
        readonly IEventStoreConnection _connection;

        readonly StreamMetaCache _cache;

        public ScavengedEventsFilter(IEventStoreConnection connection, StreamMetaCache cache) {
            _connection = connection;
            _cache = cache;
        }

        public async ValueTask<bool> Filter(
            BaseOriginalEvent originalEvent
        ) {
            if (originalEvent is not OriginalEvent) return true;
            
            var meta = await _cache.GetOrAddStreamMeta(
                originalEvent.EventDetails.Stream,
                _connection.GetStreamMeta
            ).ConfigureAwait(false);
            var isDeleted = meta.IsDeleted && meta.DeletedAt > originalEvent.Created;
            return !isDeleted && !Truncated() && !TtlExpired() && !await OverMaxCount().ConfigureAwait(false);
            
            bool TtlExpired()
                => meta.MaxAge.HasValue && originalEvent.Created < DateTime.Now - meta.MaxAge;

            bool Truncated()
                => meta.TruncateBefore.HasValue && originalEvent.Position.EventNumber < meta.TruncateBefore;

            // add the check timestamp, so we can check again if we get newer events (edge case)
            async Task<bool> OverMaxCount() {
                if (!meta.MaxCount.HasValue) return false;
                
                var streamSize = await _cache.GetOrAddStreamSize(
                    originalEvent.EventDetails.Stream,
                    _connection.GetStreamSize
                ).ConfigureAwait(false);

                return originalEvent.Position.EventNumber < streamSize.LastEventNumber - meta.MaxCount;
            }
        }
    }
}
