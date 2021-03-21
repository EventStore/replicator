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
            if (!(originalEvent is OriginalEvent)) return true;
            
            var meta = await _cache.GetOrAddStreamMeta(
                originalEvent.EventDetails.Stream,
                _connection.GetStreamMeta
            );
            return !meta.IsDeleted && !TtlExpired() && !await OverMaxCount();
            
            bool TtlExpired()
                => meta.MaxAge.HasValue && originalEvent.Created < DateTime.Now - meta.MaxAge;

            // add the check timestamp, so we can check again if we get newer events (edge case)
            async Task<bool> OverMaxCount() {
                if (!meta.MaxCount.HasValue) return false;
                
                var streamSize = await _cache.GetOrAddStreamSize(
                    originalEvent.EventDetails.Stream,
                    _connection.GetStreamSize
                );

                return originalEvent.Position.EventNumber < streamSize.LastEventNumber - meta.MaxCount;
            }
        }
    }
}
