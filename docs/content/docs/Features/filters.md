---
title: "Event filters"
linkTitle: "Filters"
date: 2021-04-05
weight: 2
description: >
    Filter out events by stream name or event type, using regular expressions. Positive and negative matches are supported.
---

You may want to prevent some events from being replicated. Those could be obsolete or "wrong" events, which your system doesn't need.

We provide two filters (in addition to the special [scavenge filter](../scavenge)):
- Event type filter
- Stream name filter

Filter options:

| Option | Values | Description |
| :----- | :----- | :---------- |
| `type` | `eventType` or `streamName` | One of the available filters |
| `include` | Regular expression | Filter for allowing events to be replicated |
| `exclude` | Regular expression | Filter for preventing events from being replicated |

For example:

```yaml
replicator:
  filters:
  - type: eventType
    include: "."
    exclude: "((Bad|Wrong)\w+Event)"
```

You can configure zero or more filters. Scavenge filter is enabled by the `scavenge` setting and doesn't need to be present in the `filter` list. You can specify either `include` or `exclude` regular expression, or both. When both `include` and `exclude` regular expressions are configured, the filter will check both, so the event must match the inclusion expression and not match the exclusion expression.

{{% alert title="Tip" color="primary" %}}
You can also use [transformations](../transforms) for advanced filtering.
{{% /alert %}}
