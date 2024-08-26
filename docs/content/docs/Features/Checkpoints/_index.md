---
title: "Checkpoints"
linkTitle: "Checkpoints"
weight: 5
description: >
  Replication checkpoints
---

Replicator stores a checkpoint that represents the current position of replicated events.

This allows Replicator to resume processing after a restart, instead of starting from the beginning.

This is controlled via the `checkpointAfter` setting.

### Configuring `checkpointAfter`

The `checkpointAfter` setting can be used to control the threshold number of events that must be replicated before a checkpoint is stored, like so:

```yaml
replicator:
  checkpoint:
    checkpointAfter: 1000
```

By default, `checkpointAfter` is configured to store a checkpoint after every `1000` events replicated.

A **lower** `checkpointAfter` would mean the replication process has stronger data consistency/duplication guarantees, at the cost of performance.

For example, configuring `checkpointAfter: 1` would result in a checkpoint being stored after every replicated event, which would achieve **exactly-once** processing.

This means in the event of a crash/restart, Replicator is guaranteed to not duplicate any events in the sink database, but comes at the cost of greatly reduced write performance to the sink database.

A **higher** `checkpointAfter` would mean writes to the sink database are more performant, at the cost of data consistency/duplication guarantees.

Configuring a higher `checkpointAfter` improves write performance by ensuring Replicator is not spending so much time saving checkpoints, but introduces a risk of events being duplicated during replication to the sink database in the event of a crash and restart, where a crash ocurred inside of the checkpoint window.

Configure the `checkpointAfter` to align with your data consistency and performance requirements.

### Checkpoint seeding

Replicator supports checkpoint seeding, which allows you to start replication from a specific event number. This is optional and the default is to not seed.

```yaml
replicator:
  checkpoint:
    seeder: 
      type: none
```

When the `type` of seeder is set to `chaser`, you can seed a checkpoint store from a `chaser.chk` file, like so:

```yaml
replicator:
  checkpoint:
    seeder: 
      type: chaser
      path: "path/to/chaser.chk"
```

This is useful when you want to start replication from the same event number as a backup's `chaser.chk`. It's not recommended to use this feature unless you are sure the `chaser.chk` file is immutable. This implies that the `chaser.chk` file of a running EventStoreDB node should not be used.
Note that seeding will only happen if the checkpoint store has no corresponding stored checkpoint.

### Checkpoint stores

Replicator supports storing checkpoints in different stores. Only one store can be configured per Replicator instance. 

If you want to run the replication again, from the same source, using the same Replicator instance and settings, you need to delete the checkpoint from the store.

See the currently supported checkpoint stores below: