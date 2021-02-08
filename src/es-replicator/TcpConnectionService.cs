using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using Microsoft.Extensions.Hosting;

namespace es_replicator {
    public class TcpConnectionService : IHostedService {
        readonly IEventStoreConnection _connection;
        
        public TcpConnectionService(IEventStoreConnection connection) => _connection = connection;

        public Task StartAsync(CancellationToken cancellationToken) {
            return _connection.ConnectAsync();
        }

        public Task StopAsync(CancellationToken  cancellationToken) {
            _connection.Close();
            return Task.CompletedTask;
        }
    }
}