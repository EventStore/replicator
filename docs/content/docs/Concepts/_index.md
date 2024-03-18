---
title: "Concepts"
linkTitle: "Concepts"
date: 2021-03-05
weight: 2
description: >
  Find out some basic concepts of Replicator.
---

Replicator is designed to copy events from one place to another, which sounds like a relatively simple task. Still, there are some concepts you need to understand before using the tool.

## Reader

Reader is an adapter for the infrastructure, where you want to copy events _from_. The _reader_ reads from a _source_.

Currently, we support readers for EventStoreDB, using TCP and gRPC protocols. Each reader type requires its own configuration, which is usually just a connection string, specific to each reader type.

The reader always reads events in sequence, but all the readers support batched reads.

There is only one reader per running Replicator instance.

## Sink and writers

Reader is an adapter for the infrastructure, where you want to copy events _to_. The _sink_ has one or more _writers_. By using multiple writers, one sink can improve performance by parallelising writes.

When using one writer for a sink, the order of events in the target remains exactly the same as it was in the source.

When using more than one writer, the global order of events in the source cannot be guaranteed. However, multiple writers also enable partitioning. The default partition key is the stream name, which guarantees the order of events in each stream.

You can only have one sink per running Replicator instance, but it might have multiple writers.

## Checkpoint

A running Replicator instance progresses linearly over a given stream of events, so it knows at any time, which events were already processed. As the process might be shut down for different reasons, it needs to maintain the last processed event position, so in case of restart, Replicator will start from there, and not from the very beginning. This way, you don't get duplicated events in the sink, and you can be sure that the replication process will eventually be completed.

The location of the last processed event in the source is known as _checkpoint_. Replicator supports storing the checkpoint in [different stores]({{< ref "checkpoints" >}}). If you want to run the replication again, from the same source, using the same Replicator instance, you need to delete the checkpoint file.

## Filters

As you might want to ignore some events during replication, Replicator supports different [filters]({{< ref "filters" >}}). Filters allow you to cover cases like preventing some obsolete events from being replicated, or splitting one source to two targets. In the latter case, you can run two Replicator instances with different filters, so events will be distributed to different sinks.

## Transforms

After being in production for a while, most systems accumulate legacy data. You might want to remove some of it using filters, but you might also want to keep the data in a different format. Typical scenarios include evolution of event schema, missing fields, incorrect data format, oversharing (sensitive unprotected information), etc.

These cases can be handler by using [transforms]({{< ref "transforms" >}}), which allow you to change any part of the event that comes from the source, before writing it to the sink.


