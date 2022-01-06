using EventStore.Replicator.JavaScript;

namespace EventStore.Replicator.Kafka; 

public class KafkaConfigurator : IConfigurator {
    readonly string _router;

    public string Protocol => "kafka";

    public KafkaConfigurator(string router) => _router = router;

    public IEventReader ConfigureReader(string connectionString) {
        throw new System.NotImplementedException("Kafka reader is not supported");
    }

    public IEventWriter ConfigureWriter(string connectionString)
        => new KafkaWriter(ParseKafkaConnection(connectionString), FunctionLoader.LoadFile(_router, "Router"));

    static ProducerConfig ParseKafkaConnection(string connectionString) {
        var settings = connectionString.Split(';');

        var dict = settings
            .Select(ParsePair)
            .ToDictionary(x => x.Key, x => x.Value);
        return new ProducerConfig(dict);
    }

    static KeyValuePair<string, string> ParsePair(string s) {
        var split = s.Split('=');
        return new KeyValuePair<string, string>(split[0].Trim(), split[1].Trim());
    }
}