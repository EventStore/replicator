using Microsoft.Extensions.Hosting;

namespace EventStore.Replicator.Esdb.Tcp; 

public class TcpConfigurator : IConfigurator {
    readonly int                _pageSize;

    public string Protocol => "tcp";

    public TcpConfigurator(int pageSize) => _pageSize = pageSize;

    public IEventReader ConfigureReader(string connectionString)
        => new TcpEventReader(
            ConfigureEventStoreTcp(connectionString, true),
            _pageSize
        );

    public IEventWriter ConfigureWriter(string connectionString)
        => new TcpEventWriter(
            ConfigureEventStoreTcp(connectionString, false)
        );

    IEventStoreConnection ConfigureEventStoreTcp(string connectionString, bool follower) {
        var builder = ConnectionSettings.Create()
            .UseCustomLogger(new TcpClientLogger())
            .KeepReconnecting()
            .KeepRetrying();

        if (follower)
            builder = builder.PreferFollowerNode();

        var connection = EventStoreConnection.Create(connectionString, builder);
        return connection;
    }
}