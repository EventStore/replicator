using Esprima;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using Jint;
using Jint.Native;
using Jint.Native.Json;
using Jint.Native.Object;
using JsonSerializer = System.Text.Json.JsonSerializer;
// ReSharper disable NotAccessedPositionalProperty.Local

namespace EventStore.Replicator.JavaScript; 

public class JsTransform {
    static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    readonly TypedJsFunction<TransformEvent, TransformedEvent?> _function;

    public JsTransform(string jsFunc) {
        _function = new TypedJsFunction<TransformEvent, TransformedEvent?>(
            jsFunc,
            "transform",
            HandleResponse
        );

        static TransformedEvent? HandleResponse(JsValue? result, TransformEvent original) {
            if (result == null || result.IsUndefined()) {
                Log.Debug("Got empty response, ignoring");
                return null;
            }

            ObjectInstance obj = result.AsObject();

            if (!TryGetString("Stream", true, out var stream) ||
                string.IsNullOrWhiteSpace(stream) ||
                !TryGetString("EventType", true, out var eventType) ||
                string.IsNullOrWhiteSpace(eventType))
                return null;

            var data = GetSerializedObject("Data");
            if (data == null) return null;

            var meta = GetSerializedObject("Meta");

            return new TransformedEvent(stream, eventType, data, meta);

            byte[]? GetSerializedObject(string propName) {
                var candidate = obj.Get(propName);

                if (candidate == null || !candidate.IsObject()) {
                    return null;
                }

                return JsonSerializer.SerializeToUtf8Bytes(candidate.ToObject());
            }

            bool TryGetString(string propName, bool log, out string value) {
                var candidate = obj.Get(propName);

                if (candidate == null || !candidate.IsString()) {
                    if (log) Log.Debug("Transformed object property {Prop} is null or not a string", propName);
                    value = string.Empty;
                    return false;
                }

                value = candidate.AsString();
                return true;
            }
        }
    }

    static readonly ParserOptions ParserOptions = new() { Tolerant = true };

    public ValueTask<BaseProposedEvent> Transform( OriginalEvent original, CancellationToken cancellationToken ) {
        var parser = new JsonParser(_function.Engine);

        var result = _function.Execute(
            new TransformEvent(
                original.Created,
                original.EventDetails.Stream,
                original.EventDetails.EventType,
                parser.Parse(original.Data.AsUtf8String()),
                ParseMeta()
            )
        );

        BaseProposedEvent evt = result == null
            ? new IgnoredEvent(original.EventDetails, original.Position, original.SequenceNumber)
            : new ProposedEvent(
                original.EventDetails with {Stream = result.Stream, EventType = result.EventType},
                result.Data,
                result.Meta,
                original.Position,
                original.SequenceNumber
            );
        return new ValueTask<BaseProposedEvent>(evt);

        JsValue? ParseMeta() {
            if (original.Metadata == null) return null;
            var metaString = original.Metadata.AsUtf8String();

            try {
                return metaString.Length == 0 ? null : parser.Parse(metaString, ParserOptions);
            }
            catch (Exception) {
                return null;
            }
        }
    }

    record TransformEvent(
        DateTimeOffset Created,
        string         Stream,
        string         EventType,
        JsValue?       Data,
        JsValue?       Meta
    );

    record TransformedEvent(
        string  Stream,
        string  EventType,
        byte[]  Data,
        byte[]? Meta
    );
}