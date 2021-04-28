using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared.Contracts;
using Jint;
using Jint.Native;

namespace EventStore.Replicator.Kafka {
    public class KafkaJsMessageRouter {
        readonly TypedJsFunction<TransformEvent, MessageRoute> _function;

        public KafkaJsMessageRouter(string routingFunction) {
            _function = new TypedJsFunction<TransformEvent, MessageRoute>(
                routingFunction,
                "route",
                AsRoute
            );

            static MessageRoute AsRoute(JsValue result, TransformEvent evt) {
                if (result == null || result.IsUndefined() || !result.IsObject())
                    return DefaultRouters.RouteByCategory(evt.Stream);

                var obj = result.AsObject();

                return new MessageRoute(
                    obj.Get("topic").AsString(),
                    obj.Get("partitionKey").AsString()
                );
            }
        }

        public MessageRoute Route(ProposedEvent evt)
            => _function.Execute(
                new TransformEvent(
                    evt.EventDetails.Stream,
                    evt.EventDetails.EventType,
                    evt.Data.AsUtf8String(),
                    evt.Metadata?.AsUtf8String()
                )
            );
    }

    record TransformEvent(string Stream, string EventType, string Data, string? Meta);

    public record MessageRoute(string Topic, string PartitionKey);
}