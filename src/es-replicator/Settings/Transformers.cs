using EventStore.Replicator.Http;
using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;

namespace es_replicator.Settings; 

public static class Transformers {
    public static TransformEvent GetTransformer(Replicator settings) {
        var type = settings.Transform?.Type;

        return type switch {
            "default" => Transforms.DefaultWithExtraMeta,
            "http"    => GetHttpTransform().Transform,
            "js"      => GetJsTransform().Transform,
            _         => Transforms.DefaultWithExtraMeta,
        };

        HttpTransform GetHttpTransform() {
            Ensure.NotEmpty(settings.Transform?.Config, "Transform config");

            return new HttpTransform(settings.Transform!.Config);
        }

        JsTransform GetJsTransform() {
            Ensure.NotEmpty(settings.Transform?.Config, "Transform config");

            var fileName = settings.Transform!.Config;
            if (!File.Exists(fileName))
                throw new ArgumentException($"JavaScript file {fileName} not found");

            var js = File.ReadAllText(fileName);
            return new JsTransform(js);
        }
    }
}