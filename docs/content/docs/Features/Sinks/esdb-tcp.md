---
title: "EventStoreDB TCP Sink"
linkTitle: "EventStoreDB TCP"
date: 2021-05-27
weight: 5
description: >
    EventStoreDB sink with TCP protocol, supported by older versions (v5 and earlier).
---

The TCP sink should only be used when migrating from one older version cluster to another older version cluster. As Event Store plans to phase out the TCP client and protocol, consider using the [gRPC sink]({{< ref "esdb-grpc" >}}) instead.

For the TCP sink, you need to specify two configurations options for it:

- `replicator.sink.protocol` - set to `tcp`
- `replicator.sink.connectionString` - use the target cluster connection string, which you'd use for the TCP client.

Check the connection string format and options in the [TCP client documentation](https://developers.eventstore.com/clients/dotnet/5.0/connecting/connection-string.html).

The risk of using the TCP sink is that you might get unstable write speed. The speed might go down when the database size grows, unlike [gRPC sink](../esdb-grpc/) write speed, which remains stable.
