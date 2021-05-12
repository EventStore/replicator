using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Read;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;
using Ubiquitous.Metrics;

namespace EventStore.Replicator {
    public static class Replicator {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public static async Task Replicate(
            IEventReader           reader,
            SinkPipeOptions        sinkPipeOptions,
            PreparePipelineOptions preparePipeOptions,
            ICheckpointStore       checkpointStore,
            CancellationToken      stoppingToken,
            bool                   restartWhenComplete = false
        ) {
            ReplicationMetrics.SetCapacity(preparePipeOptions.BufferSize, sinkPipeOptions.BufferSize);

            var cts = new CancellationTokenSource();

            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cts.Token);

            var prepareChannel = Channel.CreateBounded<PrepareContext>(preparePipeOptions.BufferSize);
            var sinkChannel    = Channel.CreateBounded<SinkContext>(sinkPipeOptions.BufferSize);

            var readerPipe = new ReaderPipe(
                reader,
                checkpointStore,
                ctx => prepareChannel.Writer.WriteAsync(ctx, ctx.CancellationToken)
            );

            var preparePipe = new PreparePipe(
                preparePipeOptions.Filter,
                preparePipeOptions.Transform,
                ctx => sinkChannel.Writer.WriteAsync(ctx, ctx.CancellationToken)
            );
            var sinkPipe = new SinkPipe(sinkPipeOptions, checkpointStore);

            var prepareTask = CreateChannelShovel(
                "Prepare",
                prepareChannel,
                preparePipe.Send,
                ReplicationMetrics.PrepareChannelSize,
                linkedCts.Token
            );

            var writerTask = CreateChannelShovel(
                "Writer",
                sinkChannel,
                sinkPipe.Send,
                ReplicationMetrics.SinkChannelSize,
                CancellationToken.None
            );
            var reporter = Task.Run(Report, stoppingToken);

            while (true) {
                ReplicationStatus.Start();

                await readerPipe.Start(linkedCts.Token).ConfigureAwait(false);

                if (!restartWhenComplete) {
                    do {
                        Log.Info("Closing the prepare channel...");
                        await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
                    } while (!prepareChannel.Writer.TryComplete());
                }

                ReplicationStatus.Stop();

                while (sinkChannel.Reader.Count > 0) {
                    await checkpointStore.Flush(CancellationToken.None).ConfigureAwait(false);
                    Log.Info("Waiting for the sink pipe to exhaust ({Left} left)...", sinkChannel.Reader.Count);
                    await Task.Delay(1000, CancellationToken.None).ConfigureAwait(false);
                }

                Log.Info("Storing the last known checkpoint");
                await checkpointStore.Flush(CancellationToken.None).ConfigureAwait(false);

                if (linkedCts.IsCancellationRequested || !restartWhenComplete) {
                    sinkChannel.Writer.Complete();
                    break;
                }

                Log.Info("Will restart in 5 sec");
                await Task.Delay(5000, stoppingToken);
            }

            try {
                await prepareTask.ConfigureAwait(false);
                await writerTask.ConfigureAwait(false);
                await reporter.ConfigureAwait(false);
            }
            catch (OperationCanceledException) { }

            static Task CreateChannelShovel<T>(
                string name, Channel<T> channel, Func<T, Task> send, IGaugeMetric size, CancellationToken token
            )
                => Task.Run(
                    () => channel.Shovel(
                        send,
                        () => Log.Info($"{name} started"),
                        () => Log.Info($"{name} stopped"),
                        size,
                        token
                    ),
                    token
                );

            async Task Report() {
                while (!stoppingToken.IsCancellationRequested) {
                    var position = await reader.GetLastPosition(stoppingToken).ConfigureAwait(false);
                    ReplicationMetrics.LastSourcePosition.Set(position);
                    await Task.Delay(5000, stoppingToken).ConfigureAwait(false);
                }
            }
        }
    }

    static class ChannelExtensions {
        public static async Task Shovel<T>(
            this Channel<T>   channel,
            Func<T, Task>     send,
            Action            beforeStart,
            Action            afterStop,
            IGaugeMetric      channelSizeGauge,
            CancellationToken token
        ) {
            beforeStart();

            try {
                while (!token.IsCancellationRequested &&
                    !channel.Reader.Completion.IsCompleted &&
                    await channel.Reader.WaitToReadAsync(token).ConfigureAwait(false)) {
                    await foreach (var ctx in channel.Reader.ReadAllAsync(token).ConfigureAwait(false)) {
                        await send(ctx).ConfigureAwait(false);
                        channelSizeGauge.Set(channel.Reader.Count);
                    }
                }
            }
            catch (OperationCanceledException) {
                // it's ok
            }

            afterStop();
        }
    }
}