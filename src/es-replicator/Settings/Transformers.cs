using System;
using System.IO;
using EventStore.Replicator.Http;
using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;

namespace es_replicator.Settings {
    public static class Transformers {
        public static TransformEvent GetTransformer(Replicator settings) {
            var type = settings.Transform?.Type;

            return type switch {
                "default" => Transforms.DefaultWithExtraMeta,
                "http"    => new HttpTransform(settings.Transform?.Config).Transform,
                "js"      => GetJsTransform().Transform,
                _         => Transforms.DefaultWithExtraMeta,
            };

            JavaScriptTransform GetJsTransform() {
                Ensure.NotEmpty(settings.Transform?.Config, "Transform config");

                var fileName = settings.Transform!.Config;
                if (!File.Exists(fileName))
                    throw new ArgumentException($"JavaScript file {fileName} not found");

                var js = File.ReadAllText(fileName);
                return new JavaScriptTransform(js);
            }
        }
    }
}