namespace EventStore.Replicator.Shared.Observe {
    public static class Counters {
        public static void SetLastRead(long position) => ReplicationMetrics.ReadEvents.Set(position);

        public static void SetLastProcessed(long position) => ReplicationMetrics.ProcessedEvents.Set(position);
    }
}