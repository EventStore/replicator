---
title: "Features"
date: 2021-03-09T20:24:47+01:00
weight: 10
---

## Scavenge

By default, Replicator would watch for events, which should not be present in the source cluster as they are considered as deleted, but not removed from the database due to lack of scavenge.

Example cases:
- The database wasn't scavenged for a while
- A stream was deleted, but not scavenged yet
- A stream was truncated either by max age or max count, but not scavenged yet

All those events will not be replicated.

## Event filters

You may want to prevent some events from being replicated. Those could be obsolete or "wrong" events, which your system doesn't need.

We provide two filters (in addition to the special scavenge filter):
- Event type filter
- Stream name filter

Filter options:

| Option | Values | Description |
| :----- | :----- | :---------- |
| `type` | `eventType` or `streamName` | One of the available filters |
| `include` | Regular expression | Filter for allowing events to be replicated |
| `exclude` | Regular expression | Filter for preventing events from being replicated |

You can configure zero or more filters. Scavenge filter is enabled by the `scavenge` setting and doesn't need to be present in the `filter` list. You can specify either `include` or `exclude` regular expression, or both. When both `include` and `exclude` regular expressions are configured, the filter will check both, so the event must match the inclusion expression and not match the exclusion expression.

## Transformations

During the replication, you might want to change the event schema. For example, some fields need to be removed, the JSON schema should change, or some data need to be merged, split, or even enriched with external data.

For this purpose, you can use the transformation function. Currently, we provide one type of transformations, which is the HTTP transformation function. Read more about it on a [dedicated page](http-transform.md). 

## Streams metadata

In addition to copying the events, Replicator will also copy streams metadata. Therefore, changes in ACLs, truncations, stream deletions, setting max count and max age will all be propagated properly to the target cluster.

However, the max age won't be working properly for events, which are going to exceed the age in the source cluster. That's because in the target cluster all the events will appear as recently added.

To mitigate the issue, Replication will add the following metadata to all the copied events:

- `$originalCreatedDate`
- `$originalEventNumber`
- `$originalEventPosition`

Note: Replicator can only add metadata to events, which don't have metadata, or have metadata in JSON format.

## Write modes

Replicator will read events from the source cluster using batched reads of 4096 events per batch. As it reads from `$all`, one batch will contain events for different streams. Therefore, writing events requires a single write operation per event to ensure the correct order of events written to the target cluster.

If you don't care much about events order in `$all`, you can configure Replicator to use concurrent writers, which will increase performance. The tool uses concurrent writers with a configurable concurrency limit. Writers are partitioned by stream name. This guarantees that events in individual streams will be in the same order as in the source cluster, but the order of `$all` will be slightly off.