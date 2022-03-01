using System.Diagnostics;
using System.Text;
using System.Text.Json;
using EventStore.Replicator.Esdb.Grpc.Internals;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using Ubiquitous.Metrics;
using Position = EventStore.Client.Position;
using StreamAcl = EventStore.Replicator.Shared.Contracts.StreamAcl;
using StreamMetadata = EventStore.Client.StreamMetadata;

namespace EventStore.Replicator.Esdb.Grpc; 

public class GrpcEventReader : IEventReader {
    public string Protocol => "grpc";
        
    readonly ILog  _log;
    readonly ILog? _debugLog;

    const string StreamDeletedBody = "{\"$tb\":9223372036854775807}";

    readonly EventStoreClient      _client;
    readonly ScavengedEventsFilter _filter;
    readonly Realtime              _realtime;

    public GrpcEventReader(EventStoreClient client) {
        _log      = LogProvider.GetCurrentClassLogger();
        _debugLog = _log.IsDebugEnabled() ? _log : null;

        _client = client;
        var metaCache = new StreamMetaCache();
        _filter   = new ScavengedEventsFilter(client, metaCache);
        _realtime = new Realtime(client, metaCache);
    }

    public async Task ReadEvents(
        Shared.Position fromPosition, Func<BaseOriginalEvent, ValueTask> next, CancellationToken cancellationToken
    ) {
        var sequence     = 0;
        var lastPosition = 0L;

        _log.Info("Starting gRPC reader");

        await _realtime.Start();

        var read = _client.ReadAllAsync(
            Direction.Forwards,
            new Position(
                fromPosition.EventPosition,
                fromPosition.EventPosition
            ),
            cancellationToken: cancellationToken
        );

        var enumerator = read.GetAsyncEnumerator(cancellationToken);

        do {
            using var activity = new Activity("read");
            activity.Start();

            var hasValue = await Metrics.Measure(
                () => enumerator.MoveNextAsync(cancellationToken),
                ReplicationMetrics.ReadsHistogram,
                ReplicationMetrics.ReadErrorsCount
            ).ConfigureAwait(false);

            if (!hasValue) break;

            var evt = enumerator.Current;
            lastPosition = (long) (evt.OriginalPosition?.CommitPosition ?? 0);

            _debugLog?.Debug(
                "gRPC: Read event with id {Id} of type {Type} from {Stream} at {Position}",
                evt.Event.EventId,
                evt.Event.EventType,
                evt.OriginalStreamId,
                evt.OriginalPosition
            );

            BaseOriginalEvent originalEvent;

            if (evt.Event.EventType == Predefined.MetadataEventType) {
                if (Encoding.UTF8.GetString(evt.Event.Data.Span) == StreamDeletedBody) {
                    originalEvent = MapStreamDeleted(
                        evt,
                        sequence++,
                        activity
                    );
                }
                else {
                    originalEvent = MapMetadata(evt, sequence++, activity);
                }
            }
            else if (evt.Event.EventType[0] != '$') {
                originalEvent = Map(evt, sequence++, activity);
            }
            else {
                await next(MapIgnored(evt, sequence++, activity)).ConfigureAwait(false);
                continue;
            }

            await next(originalEvent).ConfigureAwait(false);
        } while (true);

        _log.Info("Reached the end of the stream at {Position}", lastPosition);
    }

    public async Task<long> GetLastPosition(CancellationToken cancellationToken) {
        var events = await _client.ReadAllAsync(
            Direction.Backwards,
            Position.End,
            1,
            resolveLinkTos: false,
            cancellationToken: cancellationToken
        ).ToArrayAsync(cancellationToken).ConfigureAwait(false);
        var position = (long?) events[0].OriginalPosition?.CommitPosition;
        return position ?? 0L;
    }

    static IgnoredOriginalEvent MapIgnored(ResolvedEvent evt, int sequence, Activity activity)
        => new(
            evt.Event.Created,
            MapDetails(evt.Event),
            MapPosition(evt),
            sequence,
            new TracingMetadata(activity.TraceId, activity.SpanId)
        );

    static OriginalEvent Map(ResolvedEvent evt, int sequence, Activity activity)
        => new(
            evt.OriginalEvent.Created,
            MapDetails(evt.OriginalEvent),
            evt.OriginalEvent.Data.ToArray(),
            evt.OriginalEvent.Metadata.ToArray(),
            MapPosition(evt),
            sequence,
            new TracingMetadata(activity.TraceId, activity.SpanId)
        );

    static StreamMetadataOriginalEvent MapMetadata(ResolvedEvent evt, int sequence, Activity activity) {
        var streamMeta = JsonSerializer.Deserialize<StreamMetadata>(
            evt.Event.Data.Span,
            MetaSerialization.StreamMetadataJsonSerializerOptions
        );

        return new StreamMetadataOriginalEvent(
            evt.OriginalEvent.Created,
            MapSystemDetails(evt.OriginalEvent),
            new Shared.Contracts.StreamMetadata(
                streamMeta.MaxCount,
                streamMeta.MaxAge,
                streamMeta.TruncateBefore?.ToInt64(),
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

    static EventDetails MapDetails(EventRecord evt) =>
        new(
            evt.EventStreamId,
            evt.EventId.ToGuid(),
            evt.EventType,
            evt.ContentType
        );

    static EventDetails MapSystemDetails(EventRecord evt) =>
        new(
            evt.EventStreamId[2..],
            evt.EventId.ToGuid(),
            evt.EventType,
            ""
        );

    static Shared.Position MapPosition(ResolvedEvent evt) =>
        new(evt.OriginalEventNumber.ToInt64(), evt.OriginalPosition!.Value.CommitPosition);

    public ValueTask<bool> Filter(BaseOriginalEvent originalEvent) => _filter.Filter(originalEvent);
}