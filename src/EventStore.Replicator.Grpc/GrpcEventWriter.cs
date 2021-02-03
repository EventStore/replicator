using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Grpc {
    public class GrpcEventWriter : IEventWriter {
        readonly EventStoreClient _client;

        public GrpcEventWriter(EventStoreClient client) => _client = client;

        public Task WriteEvent(ProposedEvent proposedEvent, CancellationToken cancellationToken) {
            return _client.AppendToStreamAsync(
                proposedEvent.EventDetails.Stream,
                StreamState.Any, 
                new []{Map(proposedEvent)},
                cancellationToken: cancellationToken
            );

            static EventData Map(ProposedEvent evt)
                => new(
                    Uuid.FromGuid(evt.EventDetails.EventId),
                    evt.EventDetails.EventType,
                    evt.Data,
                    evt.Metadata,
                    evt.EventDetails.ContentType
                );
        }
    }
}
