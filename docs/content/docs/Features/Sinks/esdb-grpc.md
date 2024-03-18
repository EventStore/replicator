---
title: "EventStoreDB gRPC Sink"
linkTitle: "EventStoreDB gRPC"
date: 2021-05-27
weight: 5
description: >
    EventStoreDB sink with gRPC protocol, supported by latest versions (v20+).
---

When replicating events to Event Store Cloud, we recommend using the EventStoreDB gRPC sink.

You need to specify two configurations options for it:

- `replicator.sink.protocol` - set to `grpc`
- `replicator.sink.connectionString` - use the target cluster connection string, which you'd use for the gRPC client.

For example, for an Event Store Cloud cluster the connection string would look like:

`esdb+discover://<username>:<password>@<cluster_id>.mesdb.eventstore.cloud`.

Using gRPC gives you more predictable write operation time. For example, on a C4-size instance in Google Cloud Platform, one write would take 4-5 ms, and this number allows you to calculate the replication process throughput, as it doesn't change much when the database size grows. 
