using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<BaseProposedEvent> TransformEvent(
        OriginalEvent originalEvent, CancellationToken cancellationToken
    );

    public static class Transforms {
        public static ValueTask<ProposedEvent> Default(
            OriginalEvent originalEvent, CancellationToken _
        ) {
            return new(
                new ProposedEvent(
                    originalEvent.EventDetails,
                    originalEvent.Data,
                    originalEvent.Metadata,
                    originalEvent.Position,
                    originalEvent.SequenceNumber
                )
            );
        }

        public static ValueTask<BaseProposedEvent> DefaultWithExtraMeta(
            OriginalEvent originalEvent, CancellationToken _
        ) {
            var proposed =
                new ProposedEvent(
                    originalEvent.EventDetails,
                    originalEvent.Data,
                    AddMeta(),
                    originalEvent.Position,
                    originalEvent.SequenceNumber
                );
            return new ValueTask<BaseProposedEvent>(proposed);

            byte[] AddMeta() {
                if (originalEvent.Metadata == null || originalEvent.Metadata.Length == 0) {
                    var eventMeta = new EventMetadata {
                        OriginalEventNumber = originalEvent.Position.EventNumber,
                        OriginalPosition    = originalEvent.Position.EventPosition,
                        OriginalCreatedDate = originalEvent.Created
                    };
                    return JsonSerializer.SerializeToUtf8Bytes(eventMeta);
                }

                using var stream       = new MemoryStream();
                using var writer       = new Utf8JsonWriter(stream);
                using var originalMeta = JsonDocument.Parse(originalEvent.Metadata);

                writer.WriteStartObject();

                foreach (var jsonElement in originalMeta.RootElement.EnumerateObject()) {
                    jsonElement.WriteTo(writer);
                }
                writer.WriteNumber(EventMetadata.EventNumberPropertyName, originalEvent.Position.EventNumber);
                writer.WriteNumber(EventMetadata.PositionPropertyName, originalEvent.Position.EventPosition);
                writer.WriteString(EventMetadata.CreatedDate, originalEvent.Created);
                writer.WriteEndObject();
                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}