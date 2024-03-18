---
title: "Checkpoint file"
linkTitle: "File system"
weight: 5
description: >
    File system checkpoint store
---

By default, Replicator stores checkpoints in a local file.

The default configuration is:

```yaml
replicator:
  checkpoint:
    type: file
    path: ./checkpoint
    checkpointAfter: 1000
```

The `path` can be configured to store the checkpoint file at a different location, which can be useful for deployments to Kubernetes that may use a custom PVC configuration, for example.