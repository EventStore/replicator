using System;
using EventStore.Replicator.Http;
using EventStore.Replicator.Shared.Pipeline;

namespace es_replicator.Settings {
    public static class Transformers {
        public static TransformEvent GetTransformer(Replicator settings) {
            var type = settings.Transform.Type;

            return type switch {
                "default" => Transforms.DefaultWithExtraMeta,
                "http"    => new HttpTransform(settings.Transform.Config).Transform,
                _         => throw new ArgumentException($"Unknown transform type: {type}")
            };
        }
    }
}