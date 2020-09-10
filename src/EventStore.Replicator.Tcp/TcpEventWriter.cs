using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;

namespace EventStore.Replicator.Tcp {
    public class TcpEventWriter : IEventWriter {
        readonly IEventStoreConnection _connection;

        public TcpEventWriter(IEventStoreConnection connection) => _connection = connection;

        public Task WriteEvent(EventWrite eventWrite) {
            return _connection.AppendToStreamAsync(eventWrite.Stream, ExpectedVersion.Any, Map(eventWrite));
            
            static EventData Map(EventWrite evt) => new EventData(evt.EventId, evt.EventType, evt.IsJson, evt.Data, evt.Metadata);
        }
    }
}
