using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Observe;
using Microsoft.Extensions.Hosting;
using Ubiquitous.Metrics;
using Ubiquitous.Metrics.Labels;
using Ubiquitous.Metrics.Prometheus;

namespace es_replicator {
    public static class Measurements {
        public static void ConfigureMetrics(string environment) {
            var metrics = Metrics.CreateUsing(
                new PrometheusConfigurator(
                    new Label("app", "replicator"),
                    new Label("environment", environment)
                )
            );

            ReadEvents      = metrics.CreateGauge("reads", "Read count");
            ProcessedEvents = metrics.CreateGauge("writes", "Write count");
        }

        public static IGaugeMetric ReadEvents { get; private set; } = null!;

        public static IGaugeMetric ProcessedEvents { get; private set; } = null!;
    }

    class MeasurementService : BackgroundService {
        protected override async Task ExecuteAsync(CancellationToken stoppingToken) {
            while (!stoppingToken.IsCancellationRequested) {
                await Task.Delay(1000, stoppingToken);
                Measurements.ReadEvents.Set(Counters.LastReadEventPosition);
                Measurements.ProcessedEvents.Set(Counters.LastProcessedEventPosition);
            }
        }
    }
}