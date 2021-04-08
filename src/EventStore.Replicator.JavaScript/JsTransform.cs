using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.JavaScript {
    public class JsTransform {
        readonly TypedJsFunction<OriginalEvent, BaseProposedEvent> _function;

        public JsTransform(string jsFunc) {
            _function = new TypedJsFunction<OriginalEvent, BaseProposedEvent>(
                jsFunc,
                (script, x) => script.transform(
                    x.EventDetails.Stream,
                    x.EventDetails.EventType,
                    x.Data.AsUtf8String(),
                    x.Metadata?.AsUtf8String()
                ),
                (result, x) => HandleResponse(result, x)
            );

            static BaseProposedEvent HandleResponse(dynamic result, OriginalEvent original) {
                if (result == null || result.data == null)
                    return new IgnoredEvent(original.EventDetails, original.Position, original.SequenceNumber);
                
                string data = result.data;
                string meta = result.meta;

                return new ProposedEvent(
                    original.EventDetails with {Stream = result.stream, EventType = result.eventType},
                    data.AsUtf8Bytes(),
                    meta.AsUtf8Bytes(),
                    original.Position,
                    original.SequenceNumber
                );
            }
        }

        public ValueTask<BaseProposedEvent> Transform(
            OriginalEvent originalEvent, CancellationToken cancellationToken
        ) => new(_function.Execute(originalEvent));
    }
}