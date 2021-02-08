using System.Threading.Tasks;
using EventStore.ClientAPI;

namespace EventStore.Replicator.Tcp {
    public class Realtime {
        IEventStoreConnection _connection;

        public Realtime(IEventStoreConnection connection) {
            _connection = connection;
        }

        public async Task Start() {
            
        }
    }
}