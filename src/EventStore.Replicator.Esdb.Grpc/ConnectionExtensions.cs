using System.Linq;
using System.Threading.Tasks;
using EventStore.Client;

namespace EventStore.Replicator.Esdb.Grpc {
    static class ConnectionExtensions {
        public static async Task<StreamSize> GetStreamSize(
            this EventStoreClient client, string stream
        ) {
            var read = client.ReadStreamAsync(Direction.Backwards, stream, StreamPosition.End, 1);
            var last = await read.ToArrayAsync().ConfigureAwait(false);

            return new StreamSize(last[0].OriginalEventNumber.ToInt64());
        }

        public static async Task<StreamMeta> GetStreamMeta(
            this EventStoreClient client, string stream
        ) {
            var streamMeta = await client.GetStreamMetadataAsync(stream).ConfigureAwait(false);

            return new StreamMeta(
                streamMeta.StreamDeleted,
                streamMeta.Metadata.MaxAge,
                streamMeta.Metadata.MaxCount,
                streamMeta.MetastreamRevision!.Value.ToInt64()
            );
        }
    }
}
