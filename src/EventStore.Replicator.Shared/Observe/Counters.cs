namespace EventStore.Replicator.Shared.Observe {
    public static class Counters {
        public static long LastReadEventPosition { get; private set; }
        
        public static long LastProcessedEventPosition { get; private set; }

        public static void SetLastRead(long position) => LastReadEventPosition = position;

        public static void SetLastProcessed(long position) => LastProcessedEventPosition = position;
    }
}