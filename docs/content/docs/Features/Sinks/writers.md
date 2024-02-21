---
title: "Sink Partitioning"
linkTitle: "Sink Partitioning"
date: 2021-07-27
weight: 20
description: >
    Increase the speed of writes by enabling partitioned sinks.
---

## Write modes

Replicator will read events from the source cluster using batched reads of 4096 (default) events per batch. As it reads from `$all`, one batch will contain events for different streams. Therefore, writing events requires a single write operation per event to ensure the correct order of events written to the target cluster.

{{% alert title="Tip" color="info" %}}
You can change the batch size by setting the `replicator.reader.pageSize` setting. The maximum value is `4096`, which is also the default value. If you have large events, we recommend changing this setting. For example, you can set it to `1024`.
{{% /alert %}}

If you don't care much about events order in `$all`, you can configure Replicator to use concurrent writers, which will increase performance. The tool uses concurrent writers with a configurable concurrency limit. Writes are partitioned, and the order of written events within a partition is kept intact. Read more below about different available partitioning modes.

{{% alert title="Note" color="primary" %}}
Partitioning described on this page doesn't apply to the Kafka sink, as it uses its own routing function.
{{% /alert %}}

## Partition by stream name

Writers can be partitioned by stream name. This guarantees that events in individual streams will be in the same order as in the source cluster, but the order of `$all` will be slightly off.

To enable concurrent writers partitioned by stream name, you need to change the `replicator.sink.partitionCount` setting. The default value is `1`, so all the writes are sequential.

## Custom partitions

You can also use a JavaScript function to use event data or metadata for partitioning writers. The function must be named `partition`, it accepts a single argument, which is an object with the following schema:

```json
{
  "stream": "",
  "eventType": "",
  "data": {},
  "metadata": {}
}
```

The function must return a string, which is then used as a partition key.

For example, the following function will return the `Tenant` property of the event payload, to be used as the partition key:

```js
function partition(event) {
    return event.data.Tenant;
}
```

There are two modes for custom partitions, described below.

### Partitioning by hash

As with the stream name partitioning, the custom partition key is hashed, and the hash of the key is used to decide which partition will take the event. This method allows having less partitions than there are keys.

To use this mode you need to set the partition count using the `replicator.sink.partitionCount` setting, and also specify the file name of the partitioning function in the `replicator.sink.partitioner` setting. For example:

```yaml
replicator:
  sink:
    partitionCount: 10
    partitioner: ./partitioner.js
```

### Partition by value

In some cases, it's better to assign a single partition for each partition key. Use this method only if the number of unique values for the partition key is upper bound. This strategy works well for partitioning by tenant, for example, if the number of tenants doesn't exceed a hundred. You can also decide to go beyond this limit, but each partition uses some memory, so you need to allocate enough memory space for a high partition count. In addition, be aware of the performance concerns described in the next section. Those concerns might be less relevant though as not all the partitions will be active simultaneously if a single page doesn't contain events for all tenants at once.

To use value-based partitioning, use the same partitioning function signature. The difference is that for each returned partition key there will be a separate partition. For example, if the function deterministically return 10 different values, there will be 10 partitions. You don't need to configure the partition count, partitions will be dynamically created based on the number of unique keys.

The settings file, therefore, only needs the `replicator.sink.partitioner` setting configured.

## Partition count considerations

Do not set this setting to a very high value, as it might lead to thread starvation, or the target database overload. For example, using six to ten partitions is reasonable for a `C4` Event Store Cloud managed database, but higher value might cause degraded performance.
