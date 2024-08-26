---
title: "Configuration"
linkTitle: "Configuration"
date: 2021-03-05
weight: 5
description: >
  Replicator configuration file explained.
---

Replicator uses a configuration file in YAML format. The file must be called `appsettings.yaml` and located in the `config` subdirectory, relative to the tool working directory.

The settings file has the `replicator` root level, all settings are children to that root. It allows using the same format for the values override file when using Helm.

Available configuration options are:

| Option                                  | Description                                                                                                                                                           |
|:----------------------------------------|:----------------------------------------------------------------------------------------------------------------------------------------------------------------------|
| `replicator.reader.connectionString`    | Connection string for the source cluster or instance                                                                                                                  |
| `replicator.reader.protocol`            | Reader protocol (`tcp` or `grpc`)                                                                                                                                     |
| `replicator.reader.pageSize`            | Reader page size (only applicable for TCP protocol                                                                                                                    |
| `replicator.sink.connectionString`      | Connection string for the target cluster or instance                                                                                                                  |
| `replicator.sink.protocol`              | Writer protocol (`tcp` or `grpc`)                                                                                                                                     |
| `replicator.sink.partitionCount`        | Number of [partitioned]({{% ref "writers" %}}) concurrent writers                                                                                                     |
| `replicator.sink.partitioner`           | Custom JavaScript [partitioner]({{% ref "writers" %}})                                                                                                                |
| `replicator.sink.bufferSize`            | Size of the sink buffer, `1000` events by default                                                                                                                     |
| `replicator.scavenge`                   | Enable real-time [scavenge]({{% ref "scavenge" %}})                                                                                                                   |
| `replicator.runContinuously`            | Set to `false` if you want Replicator to stop when it reaches the end of `$all` stream. Default is `true`, so the replication continues until you stop it explicitly. |
| `replicator.filters`                    | Add one or more of provided [filters]({{% ref "filters" %}})                                                                                                          |
| `replicator.transform`                  | Configure the [event transformation]({{% ref "Transforms" %}})                                                                                                        |
| `replicator.transform.bufferSize`       | Size of the prepare buffer (filtering and transformations), `1000` events by default                                                                                  |
| `replicator.checkpoint.type`            | Type of checkpoint store (`none`, `file` or `mongo`), `none` by default                                                                                               |
| `replicator.checkpoint.path`            | The file path or connection string, empty by default                                                                                                                  |
| `replicator.checkpoint.checkpointAfter` | The number of events that must be replicated before a checkpoint is stored, `1000` events by default                                                                  |
| `replicator.checkpoint.database`        | The name of the Mongo database, `replicator` by default                                                                                                               |
| `replicator.checkpoint.instanceId`      | The name of the replicator instance to isolate checkpoints with in the Mongo database, `default` by default                                                           |
| `replicator.checkpoint.seeder.type`     | Type of checkpoint seeder to use (`none` or `chaser`), `none` by default                                                                                              |
| `replicator.checkpoint.seeder.path`     | The file path of the `chaser.chk`, empty by default                                                                                                                   |

## Enable verbose logging

You can enable debug-level logging by setting the `REPLICATOR_DEBUG` environment variable to any value.

## Example configuration

The following example configuration will instruct Replicator to read all the events from a local cluster with three nodes (`es1.acme.org`, `es2.acme.org` and `es3.acme.org`) using TCP protocol, and copy them over to the Event Store Cloud cluster with cluster ID `c2etr1lo9aeu6ojco781` using gRPC protocol. Replicator will also call an HTTP transformation function at `https://my.acme.org/transform`.

The global order of events will be the same, as `partitionCount` is set to one.

Scavenge filter is disabled, so Replicator will also copy deleted events, which haven't been scavenged by the server yet.

```yaml
replicator:
  reader:
    protocol: tcp
    connectionString: "GossipSeeds=es1.acme.org:2113,es2.acme.org:2113,es3.acme.org:2113; HeartBeatTimeout=500; DefaultUserCredentials=admin:changeit; UseSslConnection=false;"
    pageSize: 2048
  sink:
    protocol: grpc
    connectionString: "esdb://admin:changeit@c2etr1lo9aeu6ojco781.mesdb.eventstore.cloud:2113"
    partitionCount: 1
  transform:
    type: http
    config: https://my.acme.org/transform
  scavenge: false
  filters: []
  checkpoint:
    path: "./checkpoint"
```

