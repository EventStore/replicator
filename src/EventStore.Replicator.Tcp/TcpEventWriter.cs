using System;
using System.Threading;
using System.Threading.Tasks;
using EventStore.ClientAPI;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using StreamAcl = EventStore.ClientAPI.StreamAcl;
using StreamMetadata = EventStore.ClientAPI.StreamMetadata;

namespace EventStore.Replicator.Tcp {
    public class TcpEventWriter : IEventWriter {
        readonly IEventStoreConnection _connection;

        public TcpEventWriter(IEventStoreConnection connection) => _connection = connection;

        public Task WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
            Task task = proposedEvent switch
            {
                ProposedEvent p => _connection.AppendToStreamAsync(
                    p.EventDetails.Stream,
                    ExpectedVersion.Any,
                    Map(p)
                ),
                ProposedMetaEvent meta => _connection.SetStreamMetadataAsync(
                    meta.EventDetails.Stream,
                    ExpectedVersion.Any,
                    StreamMetadata.Create(
                        meta.Data.MaxCount,
                        meta.Data.MaxAge,
                        meta.Data.TruncateBefore,
                        meta.Data.CacheControl,
                        new StreamAcl(
                            meta.Data.StreamAcl.ReadRoles,
                            meta.Data.StreamAcl.WriteRoles,
                            meta.Data.StreamAcl.DeleteRoles,
                            meta.Data.StreamAcl.MetaReadRoles,
                            meta.Data.StreamAcl.MetaWriteRoles
                        )
                    )
                ),
                ProposedDeleteStream delete => _connection.DeleteStreamAsync(
                    delete.EventDetails.Stream,
                    ExpectedVersion.Any
                ),
                _ => throw new InvalidOperationException("Unknown proposed event type")
            };

            return task;

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