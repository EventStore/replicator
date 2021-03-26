using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Http {
    public class HttpTransform {
        readonly HttpClient _client;

        public HttpTransform(string url) {
            _client = new HttpClient {BaseAddress = new Uri(url)};
        }

        public async ValueTask<BaseProposedEvent> Transform(
            OriginalEvent originalEvent, CancellationToken cancellationToken
        ) {
            var httpEvent = new HttpEvent(
                originalEvent.EventDetails.EventType,
                originalEvent.EventDetails.Stream,
                Encoding.UTF8.GetString(originalEvent.Data)
            );

            try {
                var response = await _client.PostAsync(
                    "",
                    new ByteArrayContent(JsonSerializer.SerializeToUtf8Bytes(httpEvent)),
                    cancellationToken
                );

                if (!response.IsSuccessStatusCode)
                    throw new HttpRequestException($"Transformation request failed: {response.ReasonPhrase}");

                if (response.StatusCode == HttpStatusCode.NoContent)
                    return new IgnoredEvent(
                        originalEvent.EventDetails,
                        originalEvent.Position,
                        originalEvent.SequenceNumber
                    );

                var httpResponse = await JsonSerializer.DeserializeAsync<HttpEvent>(
                    await response.Content.ReadAsStreamAsync(cancellationToken),
                    cancellationToken: cancellationToken
                );

                return new ProposedEvent(
                    originalEvent.EventDetails with {
                        EventType = httpResponse.EventType, Stream = httpResponse.StreamName
                    },
                    Encoding.UTF8.GetBytes(httpResponse.Payload),
                    originalEvent.Metadata,
                    originalEvent.Position,
                    originalEvent.SequenceNumber
                );
            }
            catch (OperationCanceledException) {
                return new NoEvent(originalEvent.EventDetails, originalEvent.Position, originalEvent.SequenceNumber);
            }
        }

        record HttpEvent(string EventType, string StreamName, string Payload);
    }
}