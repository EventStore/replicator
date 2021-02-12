using EventStore.Replicator.Shared.Observe;
using Microsoft.AspNetCore.Mvc;
using Ubiquitous.Metrics;
using static EventStore.Replicator.Shared.Observe.ReplicationMetrics;

namespace es_replicator.HttpApi {
    [Route("api/counters")]
    public class Counters : ControllerBase {
        [HttpGet]
        public MetricsResponse GetCounters() {
            return new(
                ReadsHistogram.Count,
                WritesHistogram.Count,
                (long) ReadingPosition.Value,
                (long) ProcessedPosition.Value,
                (long) PrepareChannelSize.Value,
                ReadRate(),
                GetRate(WritesHistogram),
                (long) LastSourcePosition.Value,
                (long) WriterPosition.Value,
                ReplicationStatus.ReaderRunning,
                new Channel(PrepareChannelCapacity, (int) PrepareChannelSize.Value),
                new Channel(SinkChannelCapacity, (int) SinkChannelSize.Value),
                GetRate(PrepareHistogram)
            );

            static Rate GetRate(IHistogramMetric metric) => new(metric.Sum, metric.Count);

            static Rate ReadRate() => ReplicationStatus.ReaderRunning
                ? new Rate(ReplicationStatus.ElapsedSeconds, ReadsHistogram.Count)
                : new Rate(0, 0);
        }

        public record MetricsResponse(
            long ReadEvents,
            long ProcessedEvents,
            long ReadPosition,
            long WritePosition,
            long InFlightSink,
            Rate ReadRate,
            Rate WriteRate,
            long LastSourcePosition,
            long SinkPosition,
            bool ReaderRunning,
            Channel PrepareChannel,
            Channel SinkChannel,
            Rate PrepareRate
        );

        public record Rate(double Sum, long Count);

        public record Channel(int Capacity, int Size);
    }
}