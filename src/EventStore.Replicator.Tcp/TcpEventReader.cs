using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using Position = EventStore.ClientAPI.Position;

namespace EventStore.Replicator.Tcp {
    public class TcpEventReader : IEventReader {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IEventStoreConnection _connection;

        public TcpEventReader(IEventStoreConnection connection) => _connection = connection;

        public async IAsyncEnumerable<OriginalEvent> ReadEvents(
            Shared.Position fromPosition, [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            var endOfStream = false;
            var sequence    = 0;
            var start       = new Position(fromPosition.EventPosition, fromPosition.EventPosition);

            while (!cancellationToken.IsCancellationRequested) {
                var slice = await _connection.ReadAllEventsForwardAsync(start, 1024, true);

                if (slice.IsEndOfStream) {
                    if (!endOfStream)
                        Log.Info("Reached the end of the stream at {@Position}", fromPosition);

                    endOfStream = true;
                    continue;
                }

                endOfStream = false;

                foreach (var sliceEvent in slice.Events) {
                    if (sliceEvent.Event.EventType[0] == '$') continue;

                    yield return Map(sliceEvent.Event, sliceEvent.OriginalPosition!.Value, sequence++);
                }
            }

            static OriginalEvent Map(RecordedEvent evt, Position position, int sequence)
                => new(
                    evt.Created,
                    new EventDetails(
                        evt.EventStreamId,
                        evt.EventId,
                        evt.EventType,
                        evt.IsJson ? ContentTypes.Json : ContentTypes.Binary
                    ),
                    evt.Data,
                    evt.Metadata,
                    new Shared.Position(evt.EventNumber, position.CommitPosition),
                    sequence
                )
            ;
        }
    }
}
