using System;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using GreenPipes;

namespace EventStore.Replicator.Sink {
    public class SinkPipe {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
        readonly IPipe<SinkContext> _pipe;

        public SinkPipe(IEventWriter writer, ICheckpointStore checkpointStore)
            => _pipe = Pipe.New<SinkContext>(
                cfg => {
                    cfg.UseRetry(
                        r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
                    );
                    cfg.UseConcurrencyLimit(1);
                    // cfg.UsePartitioner(2, x => x.ProposedEvent.EventDetails.Stream);
                    
                    cfg.UseLog();

                    cfg.UseEventWriter(writer, Log);
                    cfg.UseCheckpointStore(checkpointStore);
                    cfg.UseExecute(ctx => Counters.SetLastProcessed(ctx.ProposedEvent.SourcePosition.EventPosition));
                }
            );

        public Task Send(SinkContext context) => _pipe.Send(context);
    }
    
    static class SinkPipelineExtensions {
        public static void UseEventWriter(this IPipeConfigurator<SinkContext> cfg, IEventWriter writer, ILog log)
            => cfg.UseExecuteAsync(
                ctx => {
                    // log.Debug("Sending to sink channel: {Event}", ctx.ProposedEvent);
                    return writer.WriteEvent(ctx.ProposedEvent, ctx.CancellationToken);
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
