using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Shared.Contracts;
using Jint.Native;
using Jint.Native.Json;

namespace EventStore.Replicator.Partitioning {
    public class JsKeyProvider {
        readonly TypedJsFunction<PartitionEvent, string?> _function;

        public JsKeyProvider(string partitionFunction) {
            _function = new TypedJsFunction<PartitionEvent, string?>(
                partitionFunction,
                "partition",
                AsPartition
            );

            static string? AsPartition(JsValue result, PartitionEvent evt)
                => result == null || result.IsUndefined() || !result.IsString()
                    ? null
                    : result.ToString();
        }

        public string GetPartitionKey(BaseProposedEvent original) {
            if (original is not ProposedEvent evt) {
                return Default();
            }

            var parser = new JsonParser(_function.Engine);

            var result = _function.Execute(
                new PartitionEvent(
                    original.EventDetails.Stream,
                    original.EventDetails.EventType,
                    parser.Parse(evt.Data.AsUtf8String()),
                    parser.Parse(evt.Metadata?.AsUtf8String())
                )
            );

            return result ?? Default();

            string Default() => KeyProvider.ByStreamName(original);
        }
    }

    record PartitionEvent(string Stream, string EventType, object Data, object? Meta);
}