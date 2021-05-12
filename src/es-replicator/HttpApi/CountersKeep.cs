using Ubiquitous.Metrics.InMemory;
using static es_replicator.Measurements;
using static EventStore.Replicator.Shared.Observe.ReplicationMetrics;

namespace es_replicator.HttpApi {
    public class CountersKeep {
        public CountersKeep() {
            PrepareChannelGauge    = GetGauge(PrepareChannelSizeName);
            SinkChannelGauge       = GetGauge(SinkChannelSizeName);
            ReadPositionGauge      = GetGauge(ReadingPositionGaugeName);
            SinkPositionGauge      = GetGauge(SinkPositionGaugeName);
            ProcessedPositionGauge = GetGauge(ProcessedPositionGaugeName);
            SourcePositionGauge    = GetGauge(LastSourcePositionGaugeName);
            ReadsHistogram         = GetHistogram(ReadsHistogramName);
            SinkHistogram          = GetHistogram(WritesHistogramName);
            PrepareHistogram       = GetHistogram(PrepareHistogramName);
        }

        public InMemoryGauge     SinkChannelGauge       { get; }
        public InMemoryGauge     SourcePositionGauge    { get; }
        public InMemoryGauge     ProcessedPositionGauge { get; }
        public InMemoryGauge     SinkPositionGauge      { get; }
        public InMemoryGauge     ReadPositionGauge      { get; }
        public InMemoryHistogram PrepareHistogram       { get; }
        public InMemoryHistogram SinkHistogram          { get; }
        public InMemoryHistogram ReadsHistogram         { get; }
        public InMemoryGauge     PrepareChannelGauge    { get; }
    }
}