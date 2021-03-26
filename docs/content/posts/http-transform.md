---
title: "HTTP Transform"
date: 2021-03-09T18:24:47+01:00
weight: 100
---

During the replication, you might want to change the event schema. For example, some fields need to be removed, the JSON schema should change, or some data need to be merged, split, or even enriched with external data.

For this purpose, you can use the transformation function. Currently, we provide one type of transformations, which is the HTTP transformation function. When it is enabled, the Replicator will call the specified HTTP(S) endpoint for each event. The endpoint is built and controlled by you. It must return a response with a transformed event with `200` status code, or `204` status code with no payload. When the Replicator receives a `204` back, it will not propagate the event, so it also works as an advanced filter.

The transformation configuration has two parameters:
- `type` - can currently be only `http`
- `config` - the HTTP(S) endpoint URL

We don't support any authentication, so the endpoint must be open and accessible for the replicator. You can host it at the same place as the Replicator itself to avoid the network latency, or elsewhere. For example, your transformation service can be running in the same Kubernetes cluster, so you can provide the internal DNS name to its service. Or, you can use a serverless function.

Replicator will call your endpoint using a `POST` request with JSON body. The request and response formats are the same:

```json
{
    "eventType": "string",
    "streamName": "string",
    "payload": "string"
}
```

The `payload` field contains the binary event data payload as UTF8 string. If you use JSON in your events, you'll get a JSON string, which you can then deserialize.

You can change any field value when returning the result. Therefore, you have full control to change the event type, stream name and the payload. If your endpoint returns 204, the event will be ignored, and we won't replicate it.

Here is an example of a serverless function in GCP, which transforms each part of the original event:

```csharp
using System.Text.Json;
using Google.Cloud.Functions.Framework;
using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace HelloWorld {
    public class Function : IHttpFunction {
        public async Task HandleAsync(HttpContext context) {
            var original = await JsonSerializer
                .DeserializeAsync<HttpEvent>(context.Request.Body);

            var payload = JsonSerializer
                .Deserialize<TestEvent>(original.Payload);
            payload.Data1 = $"Manipulated {payload.Data1}";

            var proposed = new HttpEvent {
                StreamName = $"new-{original.StreamName}",
                EventType  = $"V2.{original.EventType}",
                Payload    = JsonSerializer.Serialize(payload)
            };

            await context.Response
                .WriteAsync(JsonSerializer.Serialize(proposed));
        }

        class HttpEvent {
            public string EventType  { get; set; }
            public string StreamName { get; set; }
            public string Payload    { get; set; }
        }

        class TestEvent {
            public string Id    { get; set; }
            public string Data1 { get; set; }
            public string Data2 { get; set; }
            public string Data3 { get; set; }
        }
    }
}
```

The `TestEvent` is the original event contract, which is kept the same. However, you are free to change the event schema too, if necessary.