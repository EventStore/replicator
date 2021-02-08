using System.Collections.Generic;
using AutoDevOps;
using AutoDevOps.Addons;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;

namespace Deployment {
    public class AppStack : Stack {
        public AppStack() {
            var config   = new Config();
            var settings = new AutoDevOpsSettings(config);

            var autoDevOps = new AutoDevOps.AutoDevOps(
                settings,
                configureContainer: container => {
                    container.LivenessProbe  = HttpProbe("/health");
                    container.ReadinessProbe = HttpProbe("/ping");
                },
                configureDeployment: deployment => {
                    deployment.Metadata = deployment.Metadata.Apply(
                        x => {
                            x.Annotations.Add("sidecar.jaegertracing.io/inject", "true");
                            return x;
                        }
                    );
                },
                namespaceAnnotations: new Dictionary<string, string> {
                    {"sidecar.jaegertracing.io/inject", "true"}
                }
            );

            var unused  = Jaeger.AddJaeger(autoDevOps.DeploymentResult.Namespace!);

            ProbeArgs HttpProbe(string path) => CreateArgs.HttpProbe(path, settings.Application.Port);
        }
    }
}
