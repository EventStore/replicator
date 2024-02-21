---
title: "Kafka Sink"
linkTitle: "Apache Kafka"
date: 2021-05-27
weight: 15
description: >
    Sink for Apache Kafka, using confirmed writes
---

{{< alert type="warning" >}}Kafka sink is experimental{{< /alert >}}

The Kafka sink allows you to set up continuous replication from EventStoreDB to Apache Kafka. It might be useful, for example, to scale out subscriptions, as you can partition events in Kafka. Then, you can have a consumer group with concurrent consumers, which process individual partitions, instead of having a single partition on `$all`.

There's no way to specify a custom partition, so the default (random) Kafka partitioner will be used.

The Kafka sink needs to be configured in the `sink` section of the Replicator configuration.

- `replicator.sink.protocol` - set to `kafka`
- `replicator.sink.connectionString` - Kafka connection string, which is a comma-separated list of connection options
- `replicator.sink.partitionCount` - the number of Kafka partitions in the target topic
- `replicator.sink.router` - optional JavaScript function to route events to topics and partitions

Example:
```yaml
replicator:
  reader:
    connectionString: esdb+discover://admin:changeit@xyz.mesb.eventstore.cloud
    protocol: grpc
  sink:
    connectionString: bootstrap.servers=localhost:9092
    protocol: kafka
    partitionCount: 10
    router: ./config/route.js
```

## Routing

Replicator needs to route events to Kafka. In particular, it needs to know the topic, where to write events to, and the partition key. By default, the topic is the stream "category" (similar to the category projection), which is part of the event stream before the dash. For example, an event from `Customer-123` stream will be routed to the `Customer` topic. The stream name is used as the partition key to ensure events order within a stream. 

It's possible to customise both topic and partition key by using a routing function. You can supply a JavaScript code file, which will instruct Replicator about routing events to topics and partitions.

The code file must have a function called `route`, which accepts the following parameters:

- `stream` - original stream name
- `eventType` - original event type
- `data` - event payload (data), only works with JSON
- `metadata` - event metadata, only works with JSON

The function needs to return an object with two fields:

- `topic` - target topic
- `partitionKey` - partition key

For example:

```js
function route(stream, eventType, data, meta) {
    return {
        topic: "myTopic",
        partitionKey: stream
    }
}
```

The example function will tell Replicator to produce all the events to the `myTopic` topic, using the stream name as partition key.

You need to specify the name of the while, which contains the `route` function, in the `replicator.sink.router` setting. Such a configuration is displayed in the sample configuration YAML snipped above. 
