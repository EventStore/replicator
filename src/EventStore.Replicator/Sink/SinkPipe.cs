using System;
using System.Threading.Tasks;
using EventStore.Replicator.Observers;
using EventStore.Replicator.Partitioning;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Observe;
using GreenPipes;
using GreenPipes.Partitioning;
using Partitioner = EventStore.Replicator.Partitioning.Partitioner;

namespace EventStore.Replicator.Sink {
    public class SinkPipe {
        readonly IPipe<SinkContext> _pipe;

        public SinkPipe(SinkPipeOptions options, ICheckpointStore checkpointStore)
            => _pipe = Pipe.New<SinkContext>(
                cfg => {
                    cfg.UseRetry(
                        r => {
                            r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100));
                            r.ConnectRetryObserver(new LoggingRetryObserver());
                        }
                    );

                    if (options.PartitionCount > 1) {
                        Func<SinkContext, string> keyProvider =
                            string.IsNullOrWhiteSpace(options.Partitioner)
                                ? x => KeyProvider.ByStreamName(x.ProposedEvent)
                                : GetJsPartitioner();

                        cfg.UsePartitioner(
                            new Partitioner(
                                options.PartitionCount,
                                new Murmur3UnsafeHashGenerator()
                            ),
                            keyProvider
                        );
                    }

                    cfg.UseLog();

                    cfg.UseEventWriter(options.Writer);
                    cfg.UseCheckpointStore(checkpointStore);

                    cfg.UseExecute(
                        ctx => ReplicationMetrics.ProcessedPosition.Set(
                            ctx.ProposedEvent.SourcePosition.EventPosition
                        )
                    );

                    Func<SinkContext, string> GetJsPartitioner() {
                        var jsProvider = new JsKeyProvider(options.Partitioner!);
                        return x => jsProvider.GetPartitionKey(x.ProposedEvent);
                    }
                }
            );

        public Task Send(SinkContext context) => _pipe.Send(context);
    }

    static class SinkPipelineExtensions {
        public static void UseEventWriter(
            this IPipeConfigurator<SinkContext> cfg, IEventWriter writer
        )
            => cfg.UseExecuteAsync(
                async ctx => {
                    var position = await writer.WriteEvent(ctx.ProposedEvent, ctx.CancellationToken)
                        .ConfigureAwait(false);

                    if (position != -1)
                        ReplicationMetrics.WriterPosition.Set(position);
                }
            );

        public static void UseCheckpointStore(
            this IPipeConfigurator<SinkContext> cfg, ICheckpointStore checkpointStore
        )
            => cfg.UseExecuteAsync(
                async ctx => await checkpointStore.StoreCheckpoint(
                        ctx.ProposedEvent.SourcePosition,
                        ctx.CancellationToken
                    )
                    .ConfigureAwait(false)
            );
    }
}