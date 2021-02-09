using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
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

            var start =
                fromPosition != Shared.Position.Start
                    ? new Position(fromPosition.EventPosition, fromPosition.EventPosition)
                    : new Position(0, 0);

            while (!cancellationToken.IsCancellationRequested) {
                if (fromPosition != Shared.Position.Start) {
                    // skip one
                    var e = await _connection.ReadAllEventsForwardAsync(start, 1, false);
                    start = e.NextPosition;
                }

                using var activity = new Activity("read");
                activity.Start();

                var slice = await
                    ReplicationMetrics.Measure(
                        () => _connection.ReadAllEventsForwardAsync(start, 1024, false),
                        ReplicationMetrics.ReadsHistogram,
                        x => x.Events.Length,
                        ReplicationMetrics.ReadErrorsCount
                    );

                foreach (var sliceEvent in slice?.Events ?? Enumerable.Empty<ResolvedEvent>()) {
                    if (sliceEvent.Event.EventType.StartsWith('$') &&
                        sliceEvent.Event.EventType != Predefined.MetadataEventType)
                        continue;

                    if (Log.IsDebugEnabled())
                        Log.Debug(
                            "TCP: Read event with id {Id} of type {Type} from {Stream} at {Position}",
                            sliceEvent.Event.EventId,
                            sliceEvent.Event.EventType,
                            sliceEvent.OriginalStreamId,
                            sliceEvent.OriginalPosition
                        );

                    if (sliceEvent.Event.EventType == Predefined.MetadataEventType) {
                        if (Encoding.UTF8.GetString(sliceEvent.Event.Data) == StreamDeletedBody) {
                            if (Log.IsDebugEnabled())
                                Log.Debug("Stream deletion {Stream}", sliceEvent.Event.EventStreamId);

                            yield return MapStreamDeleted(
                                sliceEvent,
                                sequence++,
                                activity
                            );
                        }
                        else {
                            var meta = MapMetadata(sliceEvent, sequence++, activity);

                            if (Log.IsDebugEnabled())
                                Log.Debug("Stream meta {Stream}: {Meta}", sliceEvent.Event.EventStreamId, meta);

                            yield return meta;
                        }
                    }
                    else if (sliceEvent.Event.EventType[0] != '$') {
                        var originalEvent = Map(sliceEvent, sequence++, activity);

                        if (await _filter.Filter(originalEvent)) {
                            yield return originalEvent;
                        }
                    }
                }

                if (slice!.IsEndOfStream) {
                    Log.Info("Reached the end of the stream at {Position}", slice.NextPosition);
                    break;
                }

                start = slice.NextPosition;
            }
        }

        static OriginalEvent Map(ResolvedEvent evt, int sequence, Activity activity)
            => new(
                evt.OriginalEvent.Created,
                MapDetails(evt.OriginalEvent, evt.OriginalEvent.IsJson),
                evt.OriginalEvent.Data,
                evt.OriginalEvent.Metadata,
                MapPosition(evt),
                sequence,
                new TracingMetadata(activity.TraceId, activity.SpanId)
            );

        static StreamMetadataOriginalEvent MapMetadata(ResolvedEvent evt, int sequence, Activity activity) {
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
                        streamMeta.Acl?.ReadRoles,
                        streamMeta.Acl?.WriteRoles,
                        streamMeta.Acl?.DeleteRoles,
                        streamMeta.Acl?.MetaReadRoles,
                        streamMeta.Acl?.MetaWriteRoles
                    )
                ),
                MapPosition(evt),
                sequence,
                new TracingMetadata(activity.TraceId, activity.SpanId)
            );
        }

        static StreamDeletedOriginalEvent MapStreamDeleted(ResolvedEvent evt, int sequence, Activity activity)
            => new(
                evt.OriginalEvent.Created,
                MapSystemDetails(evt.OriginalEvent),
                MapPosition(evt),
                sequence,
                new TracingMetadata(activity.TraceId, activity.SpanId)
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