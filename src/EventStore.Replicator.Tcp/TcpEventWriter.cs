using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Tcp {
    public class TcpEventWriter : IEventWriter {
        readonly IEventStoreConnection _connection;

        public TcpEventWriter(IEventStoreConnection connection) => _connection = connection;

        public Task WriteEvent(ProposedEvent proposedEvent, CancellationToken cancellationToken) {
            return _connection.AppendToStreamAsync(proposedEvent.Stream, ExpectedVersion.Any, Map(proposedEvent));
            
            static EventData Map(ProposedEvent evt) => new EventData(evt.EventId, evt.EventType, evt.IsJson, evt.Data, evt.Metadata);
        }
    }
}
