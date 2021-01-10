using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Tcp.Logging;
using Position = EventStore.ClientAPI.Position;

namespace EventStore.Replicator.Tcp {
    public class TcpEventReader : IEventReader {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IEventStoreConnection _connection;

        public TcpEventReader(IEventStoreConnection connection) => _connection = connection;

        public async IAsyncEnumerable<OriginalEvent> ReadEvents(
            Shared.Position position, [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            var endOfStream = false;
            var start       = new Position(position.EventPosition, position.EventPosition);

            while (!cancellationToken.IsCancellationRequested) {
                var slice = await _connection.ReadAllEventsForwardAsync(start, 1024, true);

                if (slice.IsEndOfStream) {
                    if (!endOfStream) Log.Info("Reached the end of the stream at {@Position}", position);

                    endOfStream = true;
                    continue;
                }

                endOfStream = false;

                foreach (var sliceEvent in slice.Events) {
                    if (sliceEvent.Event.EventType[0] == '$') continue;

                    yield return Map(sliceEvent.Event, sliceEvent.OriginalPosition!.Value);
                }
            }

            static OriginalEvent Map(RecordedEvent evt, Position position)
                => new() {
                    Created       = evt.Created,
                    Data          = evt.Data,
                    Metadata      = evt.Metadata,
                    EventId       = evt.EventId,
                    EventType     = evt.EventType,
                    IsJson        = evt.IsJson,
                    EventStreamId = evt.EventStreamId,
                    Position      = new Shared.Position(evt.EventNumber, position.CommitPosition)
                };
        }
    }
}
