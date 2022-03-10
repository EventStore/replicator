using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Partitioning; 

public static class KeyProvider {
    public static string ByStreamName(BaseProposedEvent evt) => evt.EventDetails.Stream;
}