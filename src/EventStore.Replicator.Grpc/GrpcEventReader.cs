using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using Position = EventStore.Client.Position;

namespace EventStore.Replicator.Grpc {
    public class GrpcEventReader : IEventReader {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly EventStoreClient _client;

        public GrpcEventReader(EventStoreClient connection) => _client = connection;

        public async IAsyncEnumerable<BaseOriginalEvent> ReadEvents(
            Shared.Position                            fromPosition,
            [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            var sequence    = 0;

            var read = _client.ReadAllAsync(
                Direction.Forwards,
                new Position(
                    (ulong) fromPosition.EventPosition,
                    (ulong) fromPosition.EventPosition
                ),
                cancellationToken: cancellationToken
            );

            await foreach (var evt in read.WithCancellation(cancellationToken)) {
                if (evt.Event.EventStreamId.StartsWith("$")) continue;

                yield return Map(evt.Event, evt.OriginalPosition!.Value, sequence++);
            }

            Log.Info("Reached the end of the stream at {@Position}", fromPosition);

            static OriginalEvent Map(EventRecord evt, Position position, int sequence)
                => new(
                    evt.Created,
                    new EventDetails(
                        evt.EventStreamId,
                        evt.EventId.ToGuid(),
                        evt.EventType,
                        evt.ContentType
                    ),
                    evt.Data.ToArray(),
                    evt.Metadata.ToArray(),
                    new Shared.Position(evt.EventNumber.ToInt64(), (long) position.CommitPosition),
                    sequence
                )
            ;
        }
    }
}
