using EventStore.Replicator.Shared.Observe;
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
            ReplicationMetrics.Configure(metrics);
        }
    }
}