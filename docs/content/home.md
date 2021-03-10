---
title: "Introduction"
date: 2021-03-09T20:27:58+01:00
---

The Replicator tool is designed to copy (replicate) data from one EventStoreDB cluster to another without downtime. When the replication is in progress, the source cluster can be used as normal.

As a prerequisite, Replicator should run in an environment, where it can access both the source, and the target cluster. The primary supported hosting platform at this moment is a managed Kubernetes cluster in AWS, GCP or Azure.

After the Replicator starts, it will sequentially read events from the source cluster and write them to the target cluster. The process runs continuously until it reaches the end of the stream, where it reads from (usually `$all`). After write buffer gets exhausted, the replication process starts again from the last known position in the source stream.

The tool stores the last known source stream position in a configured checkpoint store. By default, it uses the file system (Persistent Volume in Kubernetes). It allows the tool to be restarted without losing the position, so it can pick up the replication from the position where it stopped.

Replication can run continuously, for unlimited time. This allows you to check the target cluster data and ensure that yu got all the data you need. When you are ready to switch your workloads, stop all the application, which write to the source cluster and configure them to connect to the new cluster instead. You don't need to stop the replication at that time, until you ensure that all the application are switched.

Pay attention to all the checkpoints you store for your catch-up subscriptions. You'd need to figure out the new checkpoint for each one of those, as it might change. For the `$all` stream, the tool will give you the corresponding commit position in the target cluster.

For persistent subscriptions, the best strategy is to ensure they finish processing all the events in the source cluster, stop the consumers, then start the consumers in the target cluster from _now_ (end of the subscription stream) before starting any writers for the target cluster.
