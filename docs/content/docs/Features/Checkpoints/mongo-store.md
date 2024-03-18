---
title: "Checkpoint in MongoDB"
linkTitle: "MongoDB"
weight: 5
description: >
    MongoDB checkpoint store
---

Although in many cases the file-based checkpoint works fine, storing the checkpoint outside the Replicator deployment is more secure. For that purpose you can use the MongoDB checkpoint store. This store writes the checkpoint as a MongoDB document to the specified database. It will unconditionally use the `checkpoint` collection.

Here are the minimum required settings for storing checkpoints in MongoDB:

```yaml
replicator:
  checkpoint:
    type: mongo
    path: "mongodb://mongoadmin:secret@localhost:27017"
    checkpointAfter: 1000
```

The `path` setting must contain a pre-authenticated connection string.

By default, Replicator will use the `replicator` database, but can be configured to use another database, like so:

```yaml
replicator:
  checkpoint:
    database: someOtherDatabase
```

If you run miltiple Replicator instances that store checkpoints in the same Mongo database, you can configure the `instanceId` setting to isolate checkpoints stored by other Replicator instances.

By default, Replicator will use `default` as the instance identifier under the assumption it is the **only** instance storing checkpoints in the database.

You may configure `instanceId` like so:

```yaml
replicator:
  checkpoint:
    instanceId: someUniqueIdentifier
```