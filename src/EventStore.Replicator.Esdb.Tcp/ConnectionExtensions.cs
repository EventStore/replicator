using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Observe;
using Ubiquitous.Metrics;

namespace EventStore.Replicator.Esdb.Tcp {
    static class ConnectionExtensions {
        public static async Task<StreamSize> GetStreamSize(
            this IEventStoreConnection connection, string stream
        ) {
            var last = await connection.ReadStreamEventsBackwardAsync(
                stream,
                StreamPosition.End,
                1,
                false
            );

            return new StreamSize(last.LastEventNumber);
        }

        public static async Task<StreamMeta> GetStreamMeta(
            this IEventStoreConnection connection, string stream
        ) {
            var streamMeta = await
                    Metrics.Measure(
                        () => connection.GetStreamMetadataAsync(stream),
                        ReplicationMetrics.MetaReadsHistogram
                    );

            return new StreamMeta(
                streamMeta.IsStreamDeleted,
                default,
                streamMeta.StreamMetadata.MaxAge,
                streamMeta.StreamMetadata.MaxCount,
                streamMeta.StreamMetadata.TruncateBefore,
                streamMeta.MetastreamVersion
            );
        }
    }
}