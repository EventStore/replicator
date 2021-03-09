---
title: "Features"
date: 2021-03-09T20:24:47+01:00
---

## Scavenge

By default, Replicator would watch for events, which should not be present in the source cluster as they are considered as deleted, but not removed from the database due to lack of scavenge.

Example cases:
- The database wasn't scavenged for a while
- A stream was deleted, but not scavenged yet
- A stream was truncated either by max age or max count, but not scavenged yet

All those events will not be replicated.

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