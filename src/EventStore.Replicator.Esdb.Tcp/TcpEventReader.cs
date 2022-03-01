using System.Diagnostics;
using System.Text;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using Position = EventStore.ClientAPI.Position;
using StreamAcl = EventStore.Replicator.Shared.Contracts.StreamAcl;
using StreamMetadata = EventStore.ClientAPI.StreamMetadata;

namespace EventStore.Replicator.Esdb.Tcp; 

public class TcpEventReader : IEventReader {
    static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    const string StreamDeletedBody = "{\"$tb\":9223372036854775807}";

    readonly IEventStoreConnection _connection;
    int                            _pageSize;
    readonly ScavengedEventsFilter _filter;
    readonly Realtime              _realtime;

    public TcpEventReader(IEventStoreConnection connection, int pageSize = 4096) {
        _connection = connection;
        _pageSize   = pageSize;
        var metaCache = new StreamMetaCache();
        _filter            =  new ScavengedEventsFilter(connection, metaCache);
        _realtime          =  new Realtime(connection, metaCache);
        _connection.Closed += HandleConnectionError;
    }

    void HandleConnectionError(object? sender, ClientClosedEventArgs args) {
        if (!args.Reason.Contains("Invalid TCP frame") || _pageSize <= 1)
            return;
            
        Log.Warn("Connection closed because of an invalid TCP frame, reducing the page size to {@PageSize}", _pageSize);
        _pageSize /= 2;
    }

    public async Task ReadEvents(
        Shared.Position fromPosition, Func<BaseOriginalEvent, ValueTask> next, CancellationToken cancellationToken
    ) {
        await _realtime.Start().ConfigureAwait(false);

        Log.Info("Starting TCP reader");

        var sequence = 0;

        var start =
            fromPosition != Shared.Position.Start
                ? new Position((long) fromPosition.EventPosition, (long) fromPosition.EventPosition)
                : new Position(0, 0);

        if (fromPosition != Shared.Position.Start) {
            // skip one
            var e = await _connection.ReadAllEventsForwardAsync(start, 1, false).ConfigureAwait(false);

            start = e.NextPosition;
        }

        while (!cancellationToken.IsCancellationRequested) {
            using var activity = new Activity("read");
            activity.Start();

            var slice = await ReplicationMetrics.Measure(
                () => _connection.ReadAllEventsForwardAsync(start, _pageSize, false),
                ReplicationMetrics.ReadsHistogram,
                x => x.Events.Length,
                ReplicationMetrics.ReadErrorsCount
            ).ConfigureAwait(false);

            foreach (var sliceEvent in slice?.Events ?? Enumerable.Empty<ResolvedEvent>()) {
                if (sliceEvent.Event.EventType.StartsWith('$') &&
                    sliceEvent.Event.EventType != Predefined.MetadataEventType) {
                    await next(MapIgnored(sliceEvent, sequence++, activity)).ConfigureAwait(false);
                    continue;
                }

                if (Log.IsDebugEnabled())
                    Log.Debug(
                        "TCP: Read event with id {Id} of type {Type} from {Stream} at {Position}",
                        sliceEvent.Event.EventId,
                        sliceEvent.Event.EventType,
                        sliceEvent.OriginalStreamId,
                        sliceEvent.OriginalPosition
                    );

                if (sliceEvent.Event.EventType == Predefined.MetadataEventType) {
                    if (sliceEvent.Event.EventStreamId.StartsWith('$')) continue;

                    if (Encoding.UTF8.GetString(sliceEvent.Event.Data) == StreamDeletedBody) {
                        if (Log.IsDebugEnabled())
                            Log.Debug("Stream deletion {Stream}", sliceEvent.Event.EventStreamId);

                        await next(MapStreamDeleted(sliceEvent, sequence++, activity));
                    }
                    else {
                        var meta = MapMetadata(sliceEvent, sequence++, activity);

                        if (Log.IsDebugEnabled())
                            Log.Debug("Stream meta {Stream}: {Meta}", sliceEvent.Event.EventStreamId, meta);

                        await next(meta);
                    }
                }
                else if (sliceEvent.Event.EventType[0] != '$') {
                    var originalEvent = Map(sliceEvent, sequence++, activity);
                    await next(originalEvent).ConfigureAwait(false);
                }
            }

            if (slice!.IsEndOfStream) {
                Log.Info("Reached the end of the stream at {Position}", slice.NextPosition);
                break;
            }

            start = slice.NextPosition;
        }
    }

    public async Task<long> GetLastPosition(CancellationToken cancellationToken) {
        var last = await _connection.ReadAllEventsBackwardAsync(Position.End, 1, false).ConfigureAwait(false);
        return last.NextPosition.CommitPosition;
    }

    public ValueTask<bool> Filter(BaseOriginalEvent originalEvent) => _filter.Filter(originalEvent);

    static IgnoredOriginalEvent MapIgnored(ResolvedEvent evt, int sequence, Activity activity)
        => new(
            evt.OriginalEvent.Created,
            MapDetails(evt.OriginalEvent, evt.OriginalEvent.IsJson),
            MapPosition(evt),
            sequence,
            new TracingMetadata(activity.TraceId, activity.SpanId)
        );

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
        new(evt.OriginalEventNumber, (ulong) evt.OriginalPosition!.Value.CommitPosition);
}