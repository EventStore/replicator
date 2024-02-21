---
title: "Additional metadata"
linkTitle: "Additional metadata"
date: 2021-04-05
weight: 4
description: >
    Additional metadata fields for the target, which include the original event number, position, and creation date.
---

In addition to copying the events, Replicator will also copy streams metadata. Therefore, changes in ACLs, truncations, stream deletions, setting max count and max age will all be propagated properly to the target cluster.

However, the max age won't be working properly for events, which are going to exceed the age in the source cluster. That's because in the target cluster all the events will appear as recently added.

To mitigate the issue, Replication will add the following metadata to all the copied events:

- `$originalCreatedDate`
- `$originalEventNumber`
- `$originalEventPosition`

{{< alert title="Note:" >}}
Replicator can only add metadata to events, which don't have metadata, or have metadata in JSON format.
{{< /alert >}}
