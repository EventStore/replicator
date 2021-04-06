using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using Microsoft.ClearScript.V8;

namespace EventStore.Replicator.JavaScript {
    public class JavaScriptTransform {
        readonly V8ScriptEngine _engine;

        public JavaScriptTransform(string jsFunc) {
            _engine = new V8ScriptEngine();
            _engine.Execute(jsFunc);
        }

        public ValueTask<BaseProposedEvent> Transform(
            OriginalEvent originalEvent, CancellationToken cancellationToken
        ) {
            var result = _engine.Script.transform(
                originalEvent.EventDetails.Stream,
                originalEvent.EventDetails.EventType,
                Encoding.UTF8.GetString(originalEvent.Data),
                originalEvent.Metadata != null ? Encoding.UTF8.GetString(originalEvent.Metadata) : null
            );

            return new ValueTask<BaseProposedEvent>(new ProposedEvent(
                originalEvent.EventDetails with {Stream = result.stream, EventType = result.eventType},
                Encoding.UTF8.GetBytes(result.data),
                Encoding.UTF8.GetBytes(result.metadata),
                originalEvent.Position,
                originalEvent.SequenceNumber
            ));
        }
    }
}