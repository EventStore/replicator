using System;
using System.Diagnostics;
using System.Threading.Tasks;
using Ubiquitous.Metrics;
using Ubiquitous.Metrics.Internals;
using Ubiquitous.Metrics.Labels;

namespace EventStore.Replicator.Shared.Observe {
    public static class ReplicationMetrics {
        public static void Configure(Metrics metrics) {
            PrepareChannelSize = metrics.CreateGauge("prepare_size", "Prepare channel size");
            SinkChannelSize    = metrics.CreateGauge("sink_size", "Sink channel size");
            ReadingPosition    = metrics.CreateGauge("read_position", "Read position");
            ProcessedPosition  = metrics.CreateGauge("processed_position", "Processed position");
            ReadsHistogram     = metrics.CreateHistogram("event_reads", "Event reads, seconds");
            MetaReadsHistogram = metrics.CreateHistogram("metadata_reads", "Stream meta reads, seconds");
            WritesHistogram    = metrics.CreateHistogram("event_writes", "Event writes, seconds");
            PrepareHistogram   = metrics.CreateHistogram("event_prepares", "Event prepares, seconds");
            ReadErrorsCount    = metrics.CreateCount("read_errors", "Reader errors count");
            WriteErrorsCount   = metrics.CreateCount("write_errors", "Sink errors count");
            LastSourcePosition = metrics.CreateGauge("last_known_position", "Last known source position");
            WriterPosition     = metrics.CreateGauge("sink_position", "Sink writer position");
        }

        public static IGaugeMetric PrepareChannelSize { get; private set; } = null!;

        public static IGaugeMetric SinkChannelSize { get; private set; } = null!;

        public static IGaugeMetric ReadingPosition { get; private set; } = null!;

        public static IGaugeMetric ProcessedPosition { get; private set; } = null!;

        public static IHistogramMetric MetaReadsHistogram { get; private set; } = null!;

        public static IHistogramMetric ReadsHistogram { get; private set; } = null!;

        public static IHistogramMetric WritesHistogram { get; private set; } = null!;

        public static ICountMetric ReadErrorsCount { get; private set; } = null!;

        public static ICountMetric WriteErrorsCount { get; private set; } = null!;

        public static IGaugeMetric LastSourcePosition { get; private set; } = null!;

        public static IGaugeMetric WriterPosition { get; private set; } = null!;

        public static IHistogramMetric PrepareHistogram { get; private set; } = null!;

        public static int PrepareChannelCapacity { get; private set; }

        public static int SinkChannelCapacity { get; private set; }

        public static async Task<T?> Measure<T>(
            Func<Task<T>>    action,
            IHistogramMetric metric,
            Func<T, int>     getCount,
            ICountMetric?    errorCount = null,
            string[]?        labels     = null
        ) where T : class {
            var stopwatch = Stopwatch.StartNew();

            T? result = null;

            try {
                result = await action();
            }
            catch (Exception) {
                errorCount?.Inc(labels: labels.ValueOrEmpty());

                throw;
            }
            finally {
                stopwatch.Stop();
                var count = result != null ? getCount(result) : 0;
                metric.Observe(stopwatch, labels, count);
            }

            return result;
        }

        public static void SetCapacity(int prepareCapacity, int sinkCapacity) {
            PrepareChannelCapacity = prepareCapacity;
            SinkChannelCapacity    = sinkCapacity;
        }
    }
}