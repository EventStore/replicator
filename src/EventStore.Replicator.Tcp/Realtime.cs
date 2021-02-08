using System;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Tcp {
    class Realtime {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
        readonly IEventStoreConnection _connection;

        readonly StreamMetaCache _metaCache;

        public Realtime(IEventStoreConnection connection, StreamMetaCache metaCache) {
            _connection = connection;
            _metaCache = metaCache;
        }

        public Task Start() {
            Log.Info("Starting realtime subscription for meta updates");
            
            return _connection.SubscribeToAllAsync(
                false,
                (_, re) => HandleEvent(re),
                HandleDrop
            );
        }

        void HandleDrop(EventStoreSubscription subscription, SubscriptionDropReason reason, Exception exception) {
            if (reason == SubscriptionDropReason.UserInitiated) return;

            Task.Run(Start);
        }

        Task HandleEvent(ResolvedEvent re) {
            if (IsSystemEvent())
                return Task.CompletedTask;

            if (IsMetadataUpdate()) {
                var stream = re.OriginalStreamId[2..];
                var meta   = StreamMetadata.FromJsonBytes(re.Event.Data);
                _metaCache.UpdateStreamMeta(stream, meta, re.OriginalEventNumber);
            }
            else {
                _metaCache.UpdateStreamLastEventNumber(re.OriginalStreamId, re.OriginalEventNumber);
            }
            return Task.CompletedTask;

            bool IsSystemEvent()
                => re.Event.EventType.StartsWith('$') && re.Event.EventType != Predefined.MetadataEventType;

            bool IsMetadataUpdate() => re.Event.EventType == Predefined.MetadataEventType;
        }
    }
}