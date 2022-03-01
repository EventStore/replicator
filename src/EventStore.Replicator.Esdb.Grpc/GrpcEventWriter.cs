using EventStore.Client;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using Ubiquitous.Metrics;
using StreamAcl = EventStore.Client.StreamAcl;
using StreamMetadata = EventStore.Client.StreamMetadata;

namespace EventStore.Replicator.Esdb.Grpc; 

public class GrpcEventWriter : IEventWriter {
    static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    readonly EventStoreClient _client;

    public GrpcEventWriter(EventStoreClient client) => _client = client;

    public async Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
        Task<long> task = proposedEvent switch {
            ProposedEvent p             => AppendEvent(p),
            ProposedDeleteStream delete => DeleteStream(delete.EventDetails.Stream),
            ProposedMetaEvent meta      => SetStreamMeta(meta),
            IgnoredEvent _              => Task.FromResult(-1L),
            _                           => throw new InvalidOperationException("Unknown proposed event type")
        };

        return
            task.IsCompleted
                ? task.Result
                : await Metrics.Measure(
                    () => task,
                    ReplicationMetrics.WritesHistogram,
                    ReplicationMetrics.WriteErrorsCount
                ).ConfigureAwait(false);

        async Task<long> AppendEvent(ProposedEvent p) {
            if (Log.IsDebugEnabled())
                Log.Debug(
                    "gRPC: Write event with id {Id} of type {Type} to {Stream} with original position {Position}",
                    p.EventDetails.EventId,
                    p.EventDetails.EventType,
                    p.EventDetails.Stream,
                    p.SourcePosition.EventPosition
                );

            var result = await _client.AppendToStreamAsync(
                proposedEvent.EventDetails.Stream,
                StreamState.Any,
                new[] {Map(p)},
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return (long) result.LogPosition.CommitPosition;
        }

        async Task<long> DeleteStream(string stream) {
            if (Log.IsDebugEnabled())
                Log.Debug("Deleting stream {Stream}", stream);

            var result = await _client.DeleteAsync(
                stream,
                StreamState.Any,
                cancellationToken: cancellationToken
            ).ConfigureAwait(false);
            return (long) result.LogPosition.CommitPosition;
        }

        async Task<long> SetStreamMeta(ProposedMetaEvent meta) {
            if (Log.IsDebugEnabled())
                Log.Debug("Setting meta for {Stream} to {Meta}", meta.EventDetails.Stream, meta);

            var result = await _client.SetStreamMetadataAsync(
                meta.EventDetails.Stream,
                StreamState.Any,
                new StreamMetadata(
                    meta.Data.MaxCount,
                    meta.Data.MaxAge,
                    ValueOrNull(meta.Data.TruncateBefore, x => new StreamPosition((ulong) x!)),
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
            ).ConfigureAwait(false);
            return (long) result.LogPosition.CommitPosition;
        }
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