---
title: "JavaScript transformation"
linkTitle: "JavaScript"
weight: 1
date: 2021-03-05
description: >
    Transform and filter events using in-proc JavaScript function
---

## Introduction

When you need to perform simple changes in the event schema, change the stream name or event type based on the existing event details and data, you can use a JavaScript transform.

{{< alert title="Warning:" >}}
JavaScript transforms only work with JSON payloads.
{{< /alert >}}

For this transform, you need to supply a code snippet, written in JavaScript, which does the transformation. The code snippet must be placed in a separate file, and it cannot have any external dependencies. There's no limitation on how complex the code is. Replicator uses the V8 engine to execute JavaScript code. Therefore, this transform normally doesn't create a lot of overhead for the replication.

## Guidelines

You can configure Replicator to use a JavaScript transformation function using the following parameters:

- `replicator.transform.type` - must be set to `js`
- `replicator.transform.config` - name of the file, which contains the transformation function

For example:

```yaml
replicator:
  transform:
    type: js
    config: ./transform.js
```

The function itself must be named `transform`. Replicator will call it with the following arguments:

- `Stream` - original stream name
- `EventType` - original event type
- `Data` - event payload as an object
- `Metadata` - event metadata as an object

The function must return an object, which contains `Stream`, `EventType`, `Data` and `Metadata` fields. Both `Data` and `Metadata` must be valid objects, the `Metadata` field can be `undefined`. If you haven't changed the event data, you can pass `Data` and `Metadata` arguments, which the function receives as arguments.

## Logging

You can log from JavaScript code directly to Replicator logs. Use the `log` object with `debug`, `info`, `warn` and `error`. You can use string interpolation as usual, or pass templated strings in [Serilog format](https://github.com/serilog/serilog/wiki/Writing-Log-Events). The first parameter is the template string, plus you can pass up to five additional values, which could be values or objects.

For example:

```javascript
log.info(
    "Transforming event {@Data} of type {Type}", 
    original.data, original.eventType
);
```

Remember that the default log level is `Information`, so debug logs won't be shown. Enable debug-level logging by setting the `REPLICATOR_DEBUG` environment variable to `true`.

## Example

Here is an example of a transformation function, which changes the event data, stream name, and event type:

```js
function transform(original) {
    // Log the transform calls
    log.info(
        "Transforming event {Type} from {Stream}", 
        original.EventType, original.Stream
    );

    // Ignore some events
    if (original.Stream.length > 7) return undefined;

    // Create a new event version
    const newEvent = {
        // Copy original data
        ...original.Data,
        // Change an existing property value 
        Data1: `new${original.Data.Data1}`,
        // Add a new property
        NewProp: `${original.Data.Id} - ${original.Data.Data2}`
    };
    
    // Return the new proposed event with modified stream and type
    return {
        Stream: `transformed${original.Stream}`,
        EventType: `V2.${original.EventType}`,
        Data: newEvent,
        Meta: original.Meta
    }
}
```

If the function returns `undefined`, the event will not be replicated, so the JavaScript transform can also be used as an advanced filter. The same happens if the transform function returns an event, but either the stream name or event type is empty or `undefined`.
