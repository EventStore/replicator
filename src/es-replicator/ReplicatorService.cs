using EventStore.Replicator;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Sink;

namespace es_replicator;

public class ReplicatorService : BackgroundService {
    readonly IEventReader           _reader;
    readonly IEventWriter           _writer;
    readonly SinkPipeOptions        _sinkOptions;
    readonly PreparePipelineOptions _prepareOptions;
    readonly ReplicatorOptions      _replicatorOptions;
    readonly ICheckpointStore       _checkpointStore;

    public ReplicatorService(
        IEventReader           reader,
        IEventWriter           writer,
        SinkPipeOptions        sinkOptions,
        PreparePipelineOptions prepareOptions,
        ReplicatorOptions      replicatorOptions,
        ICheckpointStore       checkpointStore
    ) {
        _reader            = reader;
        _writer            = writer;
        _sinkOptions       = sinkOptions;
        _prepareOptions    = prepareOptions;
        _replicatorOptions = replicatorOptions;
        _checkpointStore   = checkpointStore;
    }

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
        => Replicator.Replicate(
            _reader,
            _writer,
            _sinkOptions,
            _prepareOptions,
            _checkpointStore,
            _replicatorOptions,
            stoppingToken
        );
}