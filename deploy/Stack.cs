using System.Collections.Generic;
using AutoDevOps;
using AutoDevOps.Addons;
using Pulumi;
using Pulumi.Kubernetes.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;

namespace Deployment {
    public class AppStack : Stack {
        public AppStack() {
            var config   = new Config();
            var settings = new AutoDevOpsSettings(config);

            var claim = new PersistentVolumeClaim(
                "checkpoint-pvc",
                new PersistentVolumeClaimArgs {
                    Metadata = new ObjectMetaArgs {
                        Namespace = settings.Deploy.Namespace,
                        Name      = "checkpoint-pvc"
                    },
                    Spec = new PersistentVolumeClaimSpecArgs {
                        AccessModes = new[] {"ReadWriteOnce"},
                        Resources = new ResourceRequirementsArgs {
                            Requests = new Dictionary<string, string> {{"storage", "1Mi"}},
                        }
                    }
                }
            );

            var autoDevOps = new AutoDevOps.AutoDevOps(
                settings,
                configureContainer: container => {
                    container.LivenessProbe  = HttpProbe("/health");
                    container.ReadinessProbe = HttpProbe("/ping");

                    container.VolumeMounts = new[] {
                        new VolumeMountArgs {
                            Name      = "checkpoint-storage",
                            MountPath = "/data"
                        }
                    };
                },
                configurePod: pod => pod.Volumes = new[] {
                    new VolumeArgs {
                        Name     = "checkpoint-storage",
                        PersistentVolumeClaim = new PersistentVolumeClaimVolumeSourceArgs {
                            ClaimName = claim.Metadata.Apply(x => x.Name)
                        }
                    }
                }
                // configureDeployment: deployment => {
                //     deployment.Metadata = deployment.Metadata.Apply(
                //         x => {
                //             x.Annotations.Add("sidecar.jaegertracing.io/inject", "true");
                //             return x;
                //         }
                //     );
                // },
                // namespaceAnnotations: new Dictionary<string, string> {
                //     {"sidecar.jaegertracing.io/inject", "true"}
                // }
            );

            // var unused  = Jaeger.AddJaeger(autoDevOps.DeploymentResult.Namespace!);

            ProbeArgs HttpProbe(string path) => CreateArgs.HttpProbe(path, settings.Application.Port);
        }
    }
}