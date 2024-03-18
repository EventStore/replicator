---
title: "Kubernetes"
linkTitle: "Kubernetes"
date: 2021-03-05
description: >
  Deploying Replicator to a Kubernetes cluster
---

You can run Replicator in a Kubernetes cluster in the same cloud as your managed EventStoreDB cloud cluster. The Kubernetes cluster workloads must be able to reach the managed EventStoreDB cluster. Usually, with a proper VPC (or VN) peering between your VPC and Event Store Cloud network, it works without issues.

We provide guidelines about connecting managed Kubernetes clusters:
- [Google Kubernetes Engine](https://developers.eventstore.com/cloud/use/kubernetes/gke.html)
- [AWS EKS](https://developers.eventstore.com/cloud/use/kubernetes/eks.html)
- [Azure AKS](https://developers.eventstore.com/cloud/use/kubernetes/aks.html)
