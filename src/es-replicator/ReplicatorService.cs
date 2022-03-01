using EventStore.Replicator;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Sink;

namespace es_replicator; 

public class ReplicatorService : BackgroundService {
    readonly IEventReader           _reader;
    readonly SinkPipeOptions        _sinkOptions;
    readonly PreparePipelineOptions _prepareOptions;
    readonly ReplicatorOptions      _replicatorOptions;
    readonly ICheckpointStore       _checkpointStore;

    public ReplicatorService(
        IEventReader           reader,
        SinkPipeOptions        sinkOptions,
        PreparePipelineOptions prepareOptions,
        ReplicatorOptions      replicatorOptions,
        ICheckpointStore       checkpointStore
    ) {
        _reader            = reader;
        _sinkOptions       = sinkOptions;
        _prepareOptions    = prepareOptions;
        _replicatorOptions = replicatorOptions;
        _checkpointStore   = checkpointStore;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Replicator.Replicate(
            _reader,
            _sinkOptions,
            _prepareOptions,
            _checkpointStore,
            _replicatorOptions,
            stoppingToken
        );
}