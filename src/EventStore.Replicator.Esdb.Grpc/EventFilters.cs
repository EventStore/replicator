using System;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Esdb.Grpc {
    class ScavengedEventsFilter {
        readonly EventStoreClient _client;

        readonly StreamMetaCache _cache;

        public ScavengedEventsFilter(EventStoreClient client, StreamMetaCache cache) {
            _client = client;
            _cache = cache;
        }

        public async ValueTask<bool> Filter(
            BaseOriginalEvent originalEvent
        ) {
            var meta = await _cache.GetOrAddStreamMeta(
                originalEvent.EventDetails.Stream,
                _client.GetStreamMeta
            );
            return !meta.IsDeleted && !TtlExpired() && !await OverMaxCount();
            
            bool TtlExpired()
                => meta.MaxAge.HasValue && originalEvent.Created < DateTime.Now - meta.MaxAge;

            // add the check timestamp, so we can check again if we get newer events (edge case)
            async Task<bool> OverMaxCount() {
                if (!meta.MaxCount.HasValue) return false;
                
                var streamSize = await _cache.GetOrAddStreamSize(
                    originalEvent.EventDetails.Stream,
                    _client.GetStreamSize
                );

                return originalEvent.Position.EventNumber < streamSize.LastEventNumber - meta.MaxCount;
            }
        }
    }
}
