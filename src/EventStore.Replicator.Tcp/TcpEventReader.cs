using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Tcp.Logging;

namespace EventStore.Replicator.Tcp {
    public class TcpEventReader : IEventReader {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IEventStoreConnection _connection;

        public TcpEventReader(IEventStoreConnection connection) => _connection = connection;

        public async IAsyncEnumerable<EventRead> ReadEvents([EnumeratorCancellation] CancellationToken cancellationToken) {
            var position    = Position.Start;
            var endOfStream = false;

            while (!cancellationToken.IsCancellationRequested) {
                var slice = await _connection.ReadAllEventsForwardAsync(position, 1024, true);

                if (slice.IsEndOfStream) {
                    if (!endOfStream) Log.Info("Reached the end of the stream at {@Position}", position);

                    endOfStream = true;
                    continue;
                }

                endOfStream = false;

                foreach (var sliceEvent in slice.Events) {
                    if (sliceEvent.OriginalEvent.EventType[0] == '$') continue;

                    yield return Map(sliceEvent.Event);
                }
            }

            static EventRead Map(RecordedEvent evt)
                => new EventRead {
                    Created       = evt.Created,
                    Data          = evt.Data,
                    Metadata      = evt.Metadata,
                    EventId       = evt.EventId,
                    EventNumber   = evt.EventNumber,
                    EventType     = evt.EventType,
                    IsJson        = evt.IsJson,
                    EventStreamId = evt.EventStreamId
                };
        }
    }
}
