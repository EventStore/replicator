using System.Collections.Generic;
using Pulumi;
using Pulumi.Kubernetes.Types.Inputs.Core.V1;
using Pulumi.Kubernetes.Types.Inputs.Meta.V1;
using Ubiquitous.AutoDevOps.Stack;
using Config = Pulumi.Config;
using ConfigMap = Pulumi.Kubernetes.Core.V1.ConfigMap;
using PersistentVolumeClaim = Pulumi.Kubernetes.Core.V1.PersistentVolumeClaim;

namespace Deployment {
    public class AppStack : Stack {
        public AppStack() {
            var config   = new Config();
            var settings = new AutoDevOpsSettings(config);

            var configMap = new ConfigMap(
                "configmap",
                new ConfigMapArgs {
                    Metadata = new ObjectMetaArgs {
                        Namespace = settings.Deploy.Namespace,
                        Name      = "configuration"
                    },
                    Data = new Dictionary<string, string> {
                        {"REPLICATOR_READER_PROTOCOL", "tcp"}
                    }
                }
            );

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

            var autoDevOps = new AutoDevOps(
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

                    container.EnvFrom = new[] {
                        new EnvFromSourceArgs {
                            ConfigMapRef = new ConfigMapEnvSourceArgs {
                                Name = configMap.Metadata.Apply(x => x.Name)
                            }
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
            );

            ProbeArgs HttpProbe(string path) => CreateArgs.HttpProbe(path, settings.Application.Port);
        }
    }
}