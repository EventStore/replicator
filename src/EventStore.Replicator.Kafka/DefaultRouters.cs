using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Kafka {
    public static class DefaultRouters {
        internal static MessageRoute RouteByCategory(ProposedEvent proposedEvent) {
            var catIndex = proposedEvent.EventDetails.Stream.IndexOf('-');

            var topic = catIndex >= 0
                ? proposedEvent.EventDetails.Stream[..catIndex]
                : proposedEvent.EventDetails.Stream;

            return new MessageRoute(topic, proposedEvent.EventDetails.Stream);
        }
    }
}