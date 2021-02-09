using System;
using System.Diagnostics;
using System.Reflection.Metadata.Ecma335;
using System.Threading.Tasks;
using Ubiquitous.Metrics;
using Ubiquitous.Metrics.Internals;
using Ubiquitous.Metrics.Labels;

namespace EventStore.Replicator.Shared.Observe {
    public static class ReplicationMetrics {
        public static void Configure(Metrics metrics) {
            PrepareChannelSize = metrics.CreateGauge("prepare_size", "Prepare channel size");
            SinkChannelSize    = metrics.CreateGauge("sink_size", "Sink channel size");
            ReadEvents         = metrics.CreateGauge("read_position", "Read position");
            ProcessedEvents    = metrics.CreateGauge("write_position", "Write position");
            ReadsHistogram     = metrics.CreateHistogram("event_reads", "Event reads, seconds");
            WritesHistogram    = metrics.CreateHistogram("event_writes", "Event writes, seconds");
            ReadErrorsCount    = metrics.CreateCount("read_errors", "Reader errors count");
            WriteErrorsCount   = metrics.CreateCount("write_errors", "Sink errors count");
        }

        public static IGaugeMetric PrepareChannelSize { get; private set; } = null!;

        public static IGaugeMetric SinkChannelSize { get; private set; } = null!;

        public static IGaugeMetric ReadEvents { get; private set; } = null!;

        public static IGaugeMetric ProcessedEvents { get; private set; } = null!;

        public static IHistogramMetric ReadsHistogram { get; private set; } = null!;

        public static IHistogramMetric WritesHistogram { get; private set; } = null!;

        public static ICountMetric ReadErrorsCount { get; private set; } = null!;

        public static ICountMetric WriteErrorsCount { get; private set; } = null!;

        public static async Task<T?> Measure<T>(
            Func<Task<T>>    action,
            IHistogramMetric metric,
            Func<T, int>     getCount,
            ICountMetric?    errorCount = null,
            LabelValue[]?    labels     = null
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
                metric.Observe(stopwatch, labels, result != null ? getCount(result) : 0);
            }

            return result;
        }
    }
}