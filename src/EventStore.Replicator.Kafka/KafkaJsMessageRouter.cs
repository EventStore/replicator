using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Kafka {
    public class KafkaJsMessageRouter {
        readonly TypedJsFunction<ProposedEvent, MessageRoute> _function;

        public KafkaJsMessageRouter(string routingFunction) {
            _function = new TypedJsFunction<ProposedEvent, MessageRoute>(
                routingFunction,
                (script, evt) => script.route(
                    evt.EventDetails.Stream,
                    evt.EventDetails.EventType,
                    evt.Data.AsUtf8String(),
                    evt.Metadata?.AsUtf8String()
                ),
                (result, _) => new MessageRoute(result.topic, result.partitionKey)
            );
        }

        public MessageRoute Route(ProposedEvent evt) => _function.Execute(evt);
    }

    public record MessageRoute(string Topic, string PartitionKey);
}