using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;
using Microsoft.Extensions.Hosting;

namespace es_replicator {
    public class ReplicatorService : BackgroundService {
        readonly IEventReader      _reader;
        readonly IEventWriter      _writer;
        readonly ICheckpointStore  _checkpointStore;
        readonly FilterEvent?      _filter;
        readonly TransformEvent?   _transform;

        public ReplicatorService(
            IEventReader      reader,
            IEventWriter      writer,
            ICheckpointStore  checkpointStore,
            FilterEvent?      filter    = null,
            TransformEvent?   transform = null
        ) {
            _reader            = reader;
            _writer            = writer;
            _checkpointStore   = checkpointStore;
            _filter            = filter;
            _transform         = transform;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return new Replicator().Replicate(
                _reader,
                _writer,
                _checkpointStore,
                stoppingToken,
                _filter,
                _transform
            );
        }

    }
}
