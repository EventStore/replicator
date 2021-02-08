using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Tcp {
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
            var (isDeleted, timeSpan, maxCount) = await _cache.GetOrAddStreamMeta(
                originalEvent.EventDetails.Stream,
                _connection.GetStreamMeta
            );
            return !isDeleted && !TtlExpired() && !await OverMaxCount();
            
            bool TtlExpired()
                => timeSpan.HasValue && originalEvent.Created <
                    DateTime.Now - timeSpan;

            // add the check timestamp, so we can check again if we get newer events (edge case)
            async Task<bool> OverMaxCount() {
                var streamSize = await _cache.GetOrAddStreamSize(
                    originalEvent.EventDetails.Stream,
                    _connection.GetStreamSize
                );

                return originalEvent.Position.EventNumber < streamSize.LastEventNumber - maxCount;
            }
        }
    }
}
