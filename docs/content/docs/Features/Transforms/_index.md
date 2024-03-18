---
title: "Event transformations"
linkTitle: "Transformations"
weight: 4
date: 2021-03-05
description: >
    Transform the event schema, add or remove the data, enrich, or filter out events using complex rules.
---

During the replication, you might want to transform events using some complex rules. For example, some fields need to be removed, the JSON schema should change, or some data need to be merged, split, or even enriched with external data.

Transformations allow you:
- Move events to another stream
- Change the event type
- Manipulate the event data, like changing field names and values, or even the structure
- Same, but for metadata
- [WIP] Slit one event into multiple events

For this purpose, you can use the transformation function. Find out more about available transformation options on pages listed below.
