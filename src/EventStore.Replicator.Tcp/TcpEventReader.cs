using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using Position = EventStore.ClientAPI.Position;
using StreamAcl = EventStore.Replicator.Shared.Contracts.StreamAcl;
using StreamMetadata = EventStore.ClientAPI.StreamMetadata;

namespace EventStore.Replicator.Tcp {
    public class TcpEventReader : IEventReader {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        const string StreamDeletedBody = "{\"$tb\":9223372036854775807}";

        readonly IEventStoreConnection _connection;

        public TcpEventReader(IEventStoreConnection connection) => _connection = connection;

        public async IAsyncEnumerable<BaseOriginalEvent> ReadEvents(
            Shared.Position fromPosition, [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            var sequence = 0;
            var start    = new Position(fromPosition.EventPosition, fromPosition.EventPosition);

            while (!cancellationToken.IsCancellationRequested) {
                var slice = await _connection.ReadAllEventsForwardAsync(start, 1024, true);

                if (slice.IsEndOfStream) {
                    Log.Info("Reached the end of the stream at {@Position}", fromPosition);
                    break;
                }

                foreach (var sliceEvent in slice.Events) {
                    if (sliceEvent.Event.EventType == "$metadata") {
                        if (Encoding.UTF8.GetString(sliceEvent.Event.Data) == StreamDeletedBody) {
                            yield return MapStreamDeleted(
                                sliceEvent,
                                sequence++
                            );
                        }

                        yield return MapMetadata(sliceEvent, sequence++);
                    }

                    if (sliceEvent.Event.EventType[0] != '$') {
                        yield return Map(sliceEvent, sequence++);
                    }
                }
            }
        }

        static OriginalEvent Map(ResolvedEvent evt, int sequence)
            => new(
                evt.OriginalEvent.Created,
                MapDetails(evt.OriginalEvent, evt.OriginalEvent.IsJson),
                evt.OriginalEvent.Data,
                evt.OriginalEvent.Metadata,
                MapPosition(evt),
                sequence
            );

        static StreamMetadataOriginalEvent MapMetadata(ResolvedEvent evt, int sequence) {
            var streamMeta = StreamMetadata.FromJsonBytes(evt.OriginalEvent.Data);

            return new StreamMetadataOriginalEvent(
                evt.OriginalEvent.Created,
                MapSystemDetails(evt.OriginalEvent),
                new Shared.Contracts.StreamMetadata(
                    streamMeta.MaxCount,
                    streamMeta.MaxAge,
                    streamMeta.TruncateBefore,
                    streamMeta.CacheControl,
                    new StreamAcl(
                        streamMeta.Acl.ReadRoles,
                        streamMeta.Acl.WriteRoles,
                        streamMeta.Acl.DeleteRoles,
                        streamMeta.Acl.MetaReadRoles,
                        streamMeta.Acl.MetaWriteRoles
                    )
                ),
                MapPosition(evt),
                sequence
            );
        }

        static StreamDeletedOriginalEvent MapStreamDeleted(ResolvedEvent evt, int sequence)
            => new(
                evt.OriginalEvent.Created,
                MapSystemDetails(evt.OriginalEvent),
                MapPosition(evt),
                sequence
            );

        static EventDetails MapDetails(RecordedEvent evt, bool isJson) =>
            new(
                evt.EventStreamId,
                evt.EventId,
                evt.EventType,
                isJson ? ContentTypes.Json : ContentTypes.Binary
            );
        
        static EventDetails MapSystemDetails(RecordedEvent evt) =>
            new(
                evt.EventStreamId.Substring(2),
                evt.EventId,
                evt.EventType,
                ""
            );

        static Shared.Position MapPosition(ResolvedEvent evt) =>
            new(evt.OriginalEventNumber, evt.OriginalPosition!.Value.CommitPosition);
    }
}