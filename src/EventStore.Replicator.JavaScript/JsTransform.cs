using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using Jint;
using Jint.Native;
using Jint.Native.Object;

namespace EventStore.Replicator.JavaScript {
    public class JsTransform {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly TypedJsFunction<OriginalEvent, BaseProposedEvent> _function;

        public JsTransform(string jsFunc) {
            _function = new TypedJsFunction<OriginalEvent, BaseProposedEvent>(
                jsFunc,
                "transform",
                (script, x) => script.Invoke(
                    x.EventDetails.Stream,
                    x.EventDetails.EventType,
                    x.Data.AsUtf8String(),
                    x.Metadata?.AsUtf8String()
                ),
                HandleResponse
            );

            static BaseProposedEvent HandleResponse(JsValue result, OriginalEvent original) {
                if (result == null || result.IsUndefined()) {
                    Log.Debug("Got empty response, ignoring");
                    return AsIgnored();
                }

                ObjectInstance obj = result.AsObject();

                var meta = TryGetString("meta", false, out var metaString)
                    ? metaString.AsUtf8Bytes()
                    : null;

                if (!TryGetString("data", true, out var data) ||
                    !TryGetString("stream", true, out var stream) ||
                    !TryGetString("eventType", true, out var eventType))
                    return AsIgnored();

                return new ProposedEvent(
                    original.EventDetails with {Stream = stream, EventType = eventType},
                    data.AsUtf8Bytes(),
                    meta,
                    original.Position,
                    original.SequenceNumber
                );

                IgnoredEvent AsIgnored() => new(original.EventDetails, original.Position, original.SequenceNumber);

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

        public ValueTask<BaseProposedEvent> Transform(
            OriginalEvent originalEvent, CancellationToken cancellationToken
        ) => new(_function.Execute(originalEvent));
    }
}