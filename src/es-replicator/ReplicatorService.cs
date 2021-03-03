using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;
using Microsoft.Extensions.Hosting;

namespace es_replicator {
    public class ReplicatorService : BackgroundService {
        readonly IEventReader     _reader;
        readonly SinkPipeOptions  _sinkOptions;
        readonly ICheckpointStore _checkpointStore;
        readonly FilterEvent?     _filter;
        readonly TransformEvent?  _transform;

        public ReplicatorService(
            IEventReader     reader,
            SinkPipeOptions  sinkOptions,
            ICheckpointStore checkpointStore,
            FilterEvent?     filter    = null,
            TransformEvent?  transform = null
        ) {
            _reader          = reader;
            _sinkOptions     = sinkOptions;
            _checkpointStore = checkpointStore;
            _filter          = filter;
            _transform       = transform;
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken) {
            return Replicator.Replicate(
                _reader,
                _sinkOptions,
                _checkpointStore,
                stoppingToken,
                true,
                _filter,
                _transform
            );
        }
    }
}