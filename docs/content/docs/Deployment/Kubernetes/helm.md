---
title: "Helm"
linkTitle: "Helm chart"
date: 2021-03-05
weight: 2
description: >
  Deploy Replicator with our Helm chart
---

The easiest way to deploy Replicator to Kubernetes is by using a provided Helm chart. On this page, you find detailed instructions for using the Replicator Helm chart.

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

## Provide configuration

Configure the Replicator options using a new `values.yml` file:

```yaml
replicator:
  reader:
    connectionString: "GossipSeeds=node1.esdb.local:2113,node2.esdb.local:2113,node3.esdb.local:2113; HeartBeatTimeout=500; UseSslConnection=False;  DefaultUserCredentials=admin:changeit;"
  sink:
    connectionString: "esdb://admin:changeit@[cloudclusterid].mesdb.eventstore.cloud:2113"
    partitionCount: 6
  filters:
    - type: eventType
      include: "."
      exclude: "((Bad|Wrong)\w+Event)"
  transform:
    type: http
    config: "http://transform.somenamespace.svc:5000"
prometheus:
  metrics: true
  operator: true
```

Available options are:

| Option | Description | Default |
| :----- | :---------- | :------ |
| `replicator.reader.connectionString` | Connection string for the source cluster or instance | nil |
| `replicator.reader.protocol` | Reader protocol | `tcp` |
| `replicator.reader.pageSize` | Reader page size (only applicable for TCP protocol | `4096` |
| `replicator.sink.connectionString` | Connection string for the target cluster or instance | nil |
| `replicator.sink.protocol` | Writer protocol | `grpc` |
| `replicator.sink.partitionCount` | Number of [partitioned]({{% ref "writers" %}}) concurrent writers | `1` |
| `replicator.sink.partitioner` | Custom JavaScript [partitioner]({{% ref "writers" %}}) | `null` |
| `replicator.sink.bufferSize` | Size of the sink buffer, in events | `1000` |
| `replicator.scavenge` | Enable real-time [scavenge]({{% ref "scavenge" %}}) | `true` |
| `replicator.runContinuously` | Set to `false` if you want Replicator to stop when it reaches the end of `$all` stream. | `true` |
| `replicator.filters` | Add one or more of provided [filters]({{% ref "filters" %}}) | `[]` |
| `replicator.transform` | Configure the [event transformation]({{% ref "Transforms" %}}) |
| `replicator.transform.bufferSize` | Size of the prepare buffer (filtering and transformations), in events | `1000` |
| `prometheus.metrics` | Enable annotations for Prometheus | `false` |
| `prometheus.operator` | Create `PodMonitor` custom resource for Prometheus Operator | `false` |
| `resources.requests.cpu` | CPU request | `250m` |
| `resources.requests.memory` | Memory request | `512Mi` |
| `resources.limits.cpu` | CPU limit | `1` |
| `resources.limits.memory` | Memory limit | `1Gi` |
| `pvc.storageClass` | Persistent volume storage class name | `null` |
| `terminationGracePeriodSeconds` | Timeout for the workload graceful shutdown, it must be long enough for the sink buffer to flush | `300` |
| `jsConfigMaps` | List of existing config maps to be used as JS code files (for JS transform, for example) | `{}` |

{{< alert title="Note:" >}}
- As Replicator uses 20.10 TCP client, you have to specify `UseSsl=false` in the connection string when connecting to an insecure cluster or instance.
- Only increase the partitions count if you don't care about the `$all` stream order (regular streams will be in order anyway)
{{< /alert >}}

You should at least provide both connection strings and ensure that workloads in your Kubernetes cluster can reach both the source and the target EventStoreDB clusters or instances.

{{% alert %}}
Read also about [monitoring]({{% ref "observe" %}}) the replicator process in Kubernetes.
{{%/ alert %}}

## Configuring a JavaScript transform

Follow the documentation to configure a [JavaScript transform](/docs/features/transforms/js/) in your `values.yml` file.

Then append the following option to your `helm install` command:
```bash
--set-file transformJs=./transform.js
```

## Configuring a custom partitioner

Follow the documentation to configure a custom [partitioner]({{% ref "writers" %}}) in your `values.yml` file.

Then append the following option to your `helm install` command:
```bash
--set-file partitionerJs=./partitioner.js
```

## Complete the deployment

When you have the `values.yml` file complete, deploy the release using Helm. Remember to set the current `kubectl` context to the cluster where you are deploying to.

```bash
helm install es-replicator \
  es-replicator/es-replicator \
  --values values.yml \
  --namespace es-replicator
```

You can choose another namespace, the namespace must exist before doing a deployment.

The replication starts immediately after the deployment, assuming that all the connection strings are correct, and the Replicator workload has network access to both source and sink EventStoreDB instances.

{{% alert color="warning" %}}
The checkpoint is stored on a persistent volume, which is provisioned as part of the Helm release. If you delete the release, the volume will be deleted by the cloud provider, and the checkpoint will be gone. If you deploy the tool again, it will start from the beginning of the `$all` stream and will produce duplicate events.
{{%/ alert %}}
