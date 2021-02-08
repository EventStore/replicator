using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Client;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using StreamAcl = EventStore.Client.StreamAcl;
using StreamMetadata = EventStore.Client.StreamMetadata;

namespace EventStore.Replicator.Grpc {
    public class GrpcEventWriter : IEventWriter {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly EventStoreClient _client;

        public GrpcEventWriter(EventStoreClient client) => _client = client;

        public Task WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
            Log.Debug(
                "gRPC: Write event with id {Id} of type {Type} to {Stream} with original position {Position}",
                proposedEvent.EventDetails.EventId,
                proposedEvent.EventDetails.EventType,
                proposedEvent.EventDetails.Stream,
                proposedEvent.SourcePosition.EventPosition
            );

            Task task = proposedEvent switch {
                ProposedEvent p => _client.AppendToStreamAsync(
                    proposedEvent.EventDetails.Stream,
                    StreamState.Any,
                    new[] {Map(p)},
                    cancellationToken: cancellationToken
                ),
                ProposedDeleteStream delete => _client.SoftDeleteAsync(
                    delete.EventDetails.Stream,
                    StreamState.Any,
                    cancellationToken: cancellationToken
                ),
                ProposedMetaEvent meta => _client.SetStreamMetadataAsync(
                    meta.EventDetails.Stream,
                    StreamState.Any,
                    new StreamMetadata(
                        meta.Data.MaxCount,
                        meta.Data.MaxAge,
                        ValueOrNull(meta.Data.TruncateBefore, x => new StreamPosition((ulong) x)),
                        meta.Data.CacheControl,
                        ValueOrNull(
                            meta.Data.StreamAcl,
                            x =>
                                new StreamAcl(
                                    x.ReadRoles,
                                    x.WriteRoles,
                                    x.DeleteRoles,
                                    x.MetaReadRoles,
                                    x.MetaWriteRoles
                                )
                        )
                    ),
                    cancellationToken: cancellationToken
                ),
                _ => throw new InvalidOperationException("Unknown proposed event type")
            };
            return task;
        }

        static EventData Map(ProposedEvent evt)
            => new(
                Uuid.FromGuid(evt.EventDetails.EventId),
                evt.EventDetails.EventType,
                evt.Data,
                evt.Metadata,
                evt.EventDetails.ContentType
            );

        static T? ValueOrNull<T1, T>(T1? source, Func<T1, T> transform)
            => source == null ? default : transform(source);
    }
}