using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace EventStore.Replicator.Tcp {
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
            var streamMeta = await connection.GetStreamMetadataAsync(stream);

            return new StreamMeta(
                streamMeta.IsStreamDeleted,
                streamMeta.StreamMetadata.MaxAge,
                streamMeta.StreamMetadata.MaxCount,
                streamMeta.MetastreamVersion
            );
        }
    }
}
