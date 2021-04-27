using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared.Contracts;
using Jint;
using Jint.Native;

namespace EventStore.Replicator.Kafka {
    public class KafkaJsMessageRouter {
        readonly TypedJsFunction<ProposedEvent, MessageRoute> _function;

        public KafkaJsMessageRouter(string routingFunction) {
            _function = new TypedJsFunction<ProposedEvent, MessageRoute>(
                routingFunction,
                "route",
                (script, evt) => script.Invoke(
                    evt.EventDetails.Stream,
                    evt.EventDetails.EventType,
                    evt.Data.AsUtf8String(),
                    evt.Metadata?.AsUtf8String()
                ),
                AsRoute
            );

            static MessageRoute AsRoute(JsValue result, ProposedEvent proposed) {
                if (result == null || result.IsUndefined() || !result.IsObject())
                    return DefaultRouters.RouteByCategory(proposed);

                var obj = result.AsObject();

                return new MessageRoute(
                    obj.Get("topic").AsString(),
                    obj.Get("partitionKey").AsString()
                );
            }
        }

        public MessageRoute Route(ProposedEvent evt) => _function.Execute(evt);
    }

    public record MessageRoute(string Topic, string PartitionKey);
}