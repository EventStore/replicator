using Microsoft.Extensions.Hosting;

namespace EventStore.Replicator.Esdb.Tcp; 

public class TcpConnectionService : IHostedService {
    readonly IEventStoreConnection _connection;
        
    public TcpConnectionService(IEventStoreConnection connection) => _connection = connection;

    public Task StartAsync(CancellationToken cancellationToken) => _connection.ConnectAsync();

    public Task StopAsync(CancellationToken cancellationToken) {
        _connection.Close();
        return Task.CompletedTask;
    }
}