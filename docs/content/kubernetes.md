---
title: "Deployment to Kubernetes"
date: 2021-03-09T20:24:47+01:00
---

You can run Replicator in a Kubernetes cluster in the same cloud as your managed EventStoreDB cloud cluster. The Kubernetes cluster workloads must be able to reach the managed EventStoreDB cluster. Usually, with a proper VPC (or VN) peering between your VPC and Event Store Cloud network, it works without issues.

[WIP] We provide guidelines about connecting managed Kubernetes clusters (GKE, AKS and EKS) to Event Store Cloud in our documentation.

Use the following steps to deploy Replicator to your Kubernetes cluster:

## Add Helm repository

Ensure you have Helm 3 installed on your machine:

```bash
$ helm version
version.BuildInfo{Version:"v3.5.2", GitCommit:"167aac70832d3a384f65f9745335e9fb40169dc2", GitTreeState:"dirty", GoVersion:"go1.15.7"}
```

If you don't have Helm, following their [installation guide](https://helm.sh/docs/intro/install/).

Add the Replicator repository:

```bash
$ helm repo add es-replicator https://eventstore.github.io/replicator
$ helm repo update
```

Configure the Replicator options using a new `values.yml` file:

```yaml
replicator:
  reader:
    connectionString: "GossipSeeds=node1.esdb.local:2113,node2.esdb.local:2113,node3.esdb.local:2113; HeartBeatTimeout=500; UseSsl=False;  DefaultUserCredentials=admin:changeit;"
  sink:
    connectionString: "esdb://admin:changeit@[cloudclusterid].mesdb.eventstore.cloud:2113"
    partitionCount: 6
prometheus:
  metrics: true
  operator: true
```

Available options are:

| Option | Description | Default |
| :----- | :---------- | :------ |
| `replicator.reader.connectionString` | Connection string for the source cluster or instance | nil |
| `replicator.reader.protocol` | Reader protocol | `tcp` |
| `replicator.sink.connectionString` | Connection string for the target cluster or instance | nil |
| `replicator.sink.protocol` | Writer protocol | `grpc` |
| `replicator.sink.partitionCount` | Number of partitioned concurrent writers | `1` |
| `replicator.scavenge` | Enable real-time scavenge | `true` |
| `prometheus.metrics` | Enable annotations for Prometheus | `false` |
| `prometheus.operator` | Create `PodMonitor` custom resource for Prometheus Operator | `false` |

**Note:**
- As Replicator uses 20.10 TCP client, you have to specify `UseSsl=false` in the connection string when connecting to an insecure cluster or instance.
- Only increase the partitions count if you don't care about the `$all` stream order (regular streams will be in order anyway)

You should at least provide both connection strings and ensure that workloads in your Kubernetes cluster can reach both the source and the target EventStoreDB clusters or instances.

If you have Prometheus in your Kubernetes cluster, we recommend enabling `prometheus.metrics` option. If the `prometheus.operator` option is set to false, the deployment will be annotated with `prometheus.io/scrape`.

If you have Prometheus managed by Prometheus Operator, the scrape annotation won't work. You can set both `prometheus.metrics` and `prometheus.operator` options to `true`, so the Helm release will include the `PodMonitor` custom resource. Make sure that your `Prometheus` custom resource is properly configured with regard to `podMonitorNamespaceSelector` and `podMonitorSelector`, so it will not ignore the Replicator pod monitor.

When you have the `values.yml` file complete, deploy the release using Helm. Remember to set the current `kubectl` context to the cluster where you are deploying to.

```bash
$ helm install es-replicator es-replicator/es-replicator -f values.yml -n es-replicator
```

You can choose another namespace, the namespace must exist before doing a deployment.

Give the deployment some time to finish. Then, you should be able to connect to the Replicator service by using port forwarding:

```bash
$ kubectl port-forward -n es-replicator svc/es-replicator 5000
```

The Replicator web interface should be then accessible via [http://localhost:5000](http://localhost:5000). The UI will display the replication progress, source read and target write positions, number of events written, and the replication gap. Note that the write rate is shown for the single writer. When you use concurrent writers, the speed will be higher than shown.

The best way to monitor the replication progress is using Prometheus and Grafana. If the pod is being properly scraped for metrics, you would be able to use the Grafana dashboard, which you can create by import it from [JSON file](/grafana-dashboard.json).

Watch out for the replication gap and ensure that it decreases.

**Note:** the checkpoint is stored on a persistent volume, which is provisioned as part of the Helm release. If you delete the release, the volume will be deleted by the cloud provider, and the checkpoint will be gone. If you deploy the tool again, it will start from the beginning of the `$all` stream and will produce duplicate events.