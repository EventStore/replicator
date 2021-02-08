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

        readonly ScavengedEventsFilter _filter;

        readonly Realtime _realtime;

        public TcpEventReader(IEventStoreConnection connection) {
            _connection = connection;
            var metaCache = new StreamMetaCache();
            _filter   = new ScavengedEventsFilter(connection, metaCache);
            _realtime = new Realtime(connection, metaCache);
        }

        public async IAsyncEnumerable<BaseOriginalEvent> ReadEvents(
            Shared.Position fromPosition, [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            await _realtime.Start();

            Log.Info("Starting TCP reader");

            var sequence = 0;
            var start    = new Position(fromPosition.EventPosition, fromPosition.EventPosition);

            while (!cancellationToken.IsCancellationRequested) {
                var slice = await _connection.ReadAllEventsForwardAsync(start, 1024, true);

                if (slice.IsEndOfStream) {
                    Log.Info("Reached the end of the stream at {@Position}", fromPosition);
                    break;
                }

                foreach (var sliceEvent in slice.Events) {
                    if (sliceEvent.OriginalStreamId.StartsWith("$")) continue;

                    Log.Debug(
                        "TCP: Read event with id {Id} of type {Type} from {Stream} at {Position}",
                        sliceEvent.Event.EventId,
                        sliceEvent.Event.EventType,
                        sliceEvent.OriginalStreamId,
                        sliceEvent.OriginalPosition
                    );

                    BaseOriginalEvent originalEvent;

                    if (sliceEvent.Event.EventType == Predefined.MetadataEventType) {
                        if (Encoding.UTF8.GetString(sliceEvent.Event.Data) == StreamDeletedBody) {
                            originalEvent = MapStreamDeleted(
                                sliceEvent,
                                sequence++
                            );
                        }
                        else {
                            originalEvent = MapMetadata(sliceEvent, sequence++);
                        }
                    }
                    else if (sliceEvent.Event.EventType[0] != '$') {
                        originalEvent = Map(sliceEvent, sequence++);
                    }
                    else {
                        continue;
                    }

                    if (await _filter.Filter(originalEvent)) {
                        yield return originalEvent;
                    }
                }

                start = slice.NextPosition;
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
                    (int?) streamMeta.MaxCount,
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
                evt.EventStreamId[2..],
                evt.EventId,
                evt.EventType,
                ""
            );

        static Shared.Position MapPosition(ResolvedEvent evt) =>
            new(evt.OriginalEventNumber, evt.OriginalPosition!.Value.CommitPosition);
    }
}