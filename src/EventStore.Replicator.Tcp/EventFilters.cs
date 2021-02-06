using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Tcp {
    public class ScavengedEventsFilter {
        readonly ConcurrentDictionary<string, StreamSize> _streamsSize = new();
        readonly ConcurrentDictionary<string, StreamMeta> _streamsMeta = new();
        
        public async ValueTask<bool> Filter(
            BaseOriginalEvent originalEvent, IEventStoreConnection connection
        ) {
            var streamMeta = await _streamsMeta.GetOrAdd(
                originalEvent.EventDetails.Stream,
                connection.GetStreamMeta
            );
            return !streamMeta.IsDeleted && !TtlExpired() && !(await OverMaxCount());
            
            bool TtlExpired()
                => streamMeta.MaxAge.HasValue && originalEvent.Created <
                    DateTime.Now - streamMeta.MaxAge;

            // add the check timestamp, so we can check again if we get newer events (edge case)
            async Task<bool> OverMaxCount() {
                var streamSize = await _streamsSize.GetOrAdd(
                    originalEvent.EventDetails.Stream,
                    connection.GetStreamSize
                );

                return originalEvent.Position.EventNumber < streamSize.FirstEventNumber;
            }
        }
    }
}
