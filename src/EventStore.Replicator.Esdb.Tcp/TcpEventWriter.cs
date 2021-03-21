using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using Ubiquitous.Metrics;
using StreamAcl = EventStore.ClientAPI.StreamAcl;
using StreamMetadata = EventStore.ClientAPI.StreamMetadata;

namespace EventStore.Replicator.Esdb.Tcp {
    public class TcpEventWriter : IEventWriter {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IEventStoreConnection _connection;

        public TcpEventWriter(IEventStoreConnection connection) => _connection = connection;

        public Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
            if (Log.IsDebugEnabled())
                Log.Debug(
                    "TCP: Write event with id {Id} of type {Type} to {Stream} with original position {Position}",
                    proposedEvent.EventDetails.EventId,
                    proposedEvent.EventDetails.EventType,
                    proposedEvent.EventDetails.Stream,
                    proposedEvent.SourcePosition.EventPosition
                );

            Task<long> task = proposedEvent switch {
                ProposedEvent p             => Append(p),
                ProposedMetaEvent meta      => SetMeta(meta),
                ProposedDeleteStream delete => Delete(delete),
                IgnoredEvent _              => Task.FromResult(-1L),
                _                           => throw new InvalidOperationException("Unknown proposed event type")
            };

            return Metrics.Measure(() => task, ReplicationMetrics.WritesHistogram, ReplicationMetrics.WriteErrorsCount);

            async Task<long> Append(ProposedEvent p) {
                var result = await _connection.AppendToStreamAsync(
                    p.EventDetails.Stream,
                    ExpectedVersion.Any,
                    Map(p)
                );
                return result.LogPosition.CommitPosition;
            }

            async Task<long> SetMeta(ProposedMetaEvent meta) {
                var result = await _connection.SetStreamMetadataAsync(
                    meta.EventDetails.Stream,
                    ExpectedVersion.Any,
                    StreamMetadata.Create(
                        meta.Data.MaxCount,
                        meta.Data.MaxAge,
                        meta.Data.TruncateBefore,
                        meta.Data.CacheControl,
                        new StreamAcl(
                            meta.Data.StreamAcl?.ReadRoles,
                            meta.Data.StreamAcl?.WriteRoles,
                            meta.Data.StreamAcl?.DeleteRoles,
                            meta.Data.StreamAcl?.MetaReadRoles,
                            meta.Data.StreamAcl?.MetaWriteRoles
                        )
                    )
                );
                return result.LogPosition.CommitPosition;
            }

            async Task<long> Delete(ProposedDeleteStream delete) {
                var result = await _connection.DeleteStreamAsync(
                    delete.EventDetails.Stream,
                    ExpectedVersion.Any
                );
                return result.LogPosition.CommitPosition;
            }

            static EventData Map(ProposedEvent evt) {
                return
                    new(
                        evt.EventDetails.EventId,
                        evt.EventDetails.EventType,
                        evt.EventDetails.ContentType == ContentTypes.Json,
                        evt.Data,
                        evt.Metadata
                    );
            }
        }
    }
}