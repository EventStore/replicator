using System.Text.Json;
using EventStore.Replicator.Esdb.Grpc.Internals;

namespace EventStore.Replicator.Esdb.Grpc; 

class Realtime {
    readonly EventStoreClient _client;

    readonly StreamMetaCache _metaCache;

    bool _started;

    public Realtime(EventStoreClient client, StreamMetaCache metaCache) {
        _client    = client;
        _metaCache = metaCache;
    }

    public Task Start() {
        if (_started) return Task.CompletedTask;

        _started = true;
        return _client.SubscribeToAllAsync(
            (_, evt, _) => HandleEvent(evt),
            subscriptionDropped: HandleDrop
        );
    }

    void HandleDrop(StreamSubscription subscription, SubscriptionDroppedReason reason, Exception? exception) {
        if (reason == SubscriptionDroppedReason.Disposed) return;

        _started = false;
        Task.Run(Start);
    }

    Task HandleEvent(ResolvedEvent re) {
        if (IsSystemEvent())
            return Task.CompletedTask;

        if (IsMetadataUpdate()) {
            var stream = re.OriginalStreamId[2..];

            var meta = JsonSerializer.Deserialize<StreamMetadata>(
                re.Event.Data.Span,
                MetaSerialization.StreamMetadataJsonSerializerOptions
            );
            _metaCache.UpdateStreamMeta(stream, meta, re.OriginalEventNumber.ToInt64());
        }
        else {
            _metaCache.UpdateStreamLastEventNumber(re.OriginalStreamId, re.OriginalEventNumber.ToInt64());
        }

        return Task.CompletedTask;

        bool IsSystemEvent()
            => re.Event.EventType.StartsWith('$') && re.Event.EventType != Predefined.MetadataEventType;

        bool IsMetadataUpdate() => re.Event.EventType == Predefined.MetadataEventType;
    }
}