using System;
using System.Threading.Tasks;
using EventStore.Replicator.Observers;
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

                    if (options.PartitionCount > 1)
                        cfg.UsePartitioner(
                            new Partitioner(options.PartitionCount, new Murmur3UnsafeHashGenerator()),
                            x => x.ProposedEvent.EventDetails.Stream
                        );

                    cfg.UseLog();

                    cfg.UseEventWriter(options.Writer);
                    cfg.UseCheckpointStore(checkpointStore);

                    cfg.UseExecute(
                        ctx => ReplicationMetrics.ProcessedPosition.Set(ctx.ProposedEvent.SourcePosition.EventPosition)
                    );
                }
            );

        public Task Send(SinkContext context) => _pipe.Send(context);
    }

    static class SinkPipelineExtensions {
        public static void UseEventWriter(this IPipeConfigurator<SinkContext> cfg, IEventWriter writer)
            => cfg.UseExecuteAsync(
                async ctx => {
                    var position = await writer.WriteEvent(ctx.ProposedEvent, ctx.CancellationToken);

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
            );
    }
}