using EventStore.Replicator.Shared;

namespace EventStore.Replicator; 

public class Factory {
    readonly List<IConfigurator> _configurators;

    public Factory(IEnumerable<IConfigurator> configurators) => _configurators = configurators.ToList();

    public IEventReader GetReader(string protocol, string connectionString)
        => GetConfigurator(protocol).ConfigureReader(connectionString);

    public IEventWriter GetWriter(string protocol, string connectionString)
        => GetConfigurator(protocol).ConfigureWriter(connectionString);

    IConfigurator GetConfigurator(string protocol) {
        var configurator = _configurators.FirstOrDefault(x => x.Protocol == protocol);

        return configurator ?? throw new NotSupportedException($"Unsupported protocol: {protocol}");
    }
}