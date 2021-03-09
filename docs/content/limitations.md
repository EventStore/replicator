---
title: "Limitations"
date: 2021-03-09T20:24:47+01:00
---

## Performance

Replicator uses conventional client protocols: TCP and gRPC. We recommend using TCP for the source clusters connection (reading) and gRPC for the target cluster.

When copying the data, we must ensure that the order of events in the target cluster remains the same. The level of this guarantee depends on the selected write mode (single writer or partitioned concurrent writers), but events are still written one by one, as that's the write mode supported by all the clients.

These factors impact the overall write performance. Considering the normal latency of a write operation via GRPC (3-6 ms, depending on the cluster instance size and the cloud provider), a single writer can only write 150-300 events per second. Event size, unless it's very big, doesn't plan much of a role for the latency figure. Partitioned writes, running concurrently, can effectively reach the speed of more than 1000 events per second. Using more than six concurrent writers would not increase the performance as the bottleneck will shift to the server.

Based on the mentioned figures, we can expect to replicate around one million events per hour with a single writer, and 3.5 million events per hour when using concurrent writers. Therefore, the tool mainly aims to help customers with small to medium size databases. Replication a multi-terabyte database with billions of events would probably never work as it won't be able to catch up with frequent writes to the source cluster.

Therefore, the important indicator of replication completion feasibility would be observing the replication gap metric provided by the tool and ensure it is lowering. If the gap stays constant or even increasing, the tool is not suitable for your database.

## Created date

The system property, which holds the timestamp when the event was physically written to the database, won't be propagated to the target cluster as it's impossible to set this value using a conventional client. To mitigate this issue, Replicator will add a metadata field `$originalCreatedDate`, which will contain the original event creation date.

Note: Replicator can only add metadata to events, which don't have metadata, or have metadata in JSON format.

## Max age stream metadata

Despite Replicator will copy all the stream metadata, the max age set on a stream won't work as expected. That's because all the events in the target cluster will get a new date. The `$originalCreatedDate` metadata field might help to mitigate the issue.
