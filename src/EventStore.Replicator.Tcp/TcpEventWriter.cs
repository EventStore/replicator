using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Tcp {
    public class TcpEventWriter : IEventWriter {
        readonly IEventStoreConnection _connection;

        public TcpEventWriter(IEventStoreConnection connection) => _connection = connection;

        public Task WriteEvent(ProposedEvent proposedEvent, CancellationToken cancellationToken) {
            return _connection.AppendToStreamAsync(
                proposedEvent.EventDetails.Stream,
                ExpectedVersion.Any,
                Map(proposedEvent)
            );

            static EventData Map(ProposedEvent evt)
                => new(
                    evt.EventDetails.EventId,
                    evt.EventDetails.EventType,
                    evt.EventDetails.ContentType == ContentTypes.Json,
                    evt.Data,
                    evt.Metadata
                );
        }
    }
}
