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

        const int Capacity = 50000;

        public static async Task Replicate(
            IEventReader      reader,
            SinkPipeOptions   sinkPipeOptions,
            ICheckpointStore  checkpointStore,
            CancellationToken stoppingToken,
            bool              restartWhenComplete = false,
            FilterEvent?      filter              = null,
            TransformEvent?   transform           = null
        ) {
            ReplicationMetrics.SetCapacity(Capacity, Capacity);

            var cts = new CancellationTokenSource();

            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cts.Token);

            var prepareChannel = Channel.CreateBounded<PrepareContext>(Capacity);
            var sinkChannel    = Channel.CreateBounded<SinkContext>(Capacity);

            var readerPipe = new ReaderPipe(
                reader,
                checkpointStore,
                ctx => prepareChannel.Writer.WriteAsync(ctx, ctx.CancellationToken)
            );

            var preparePipe = new PreparePipe(
                filter,
                transform,
                ctx => sinkChannel.Writer.WriteAsync(ctx, ctx.CancellationToken)
            );
            var sinkPipe = new SinkPipe(sinkPipeOptions, checkpointStore);

            var prepareTask = CreateChannelShovel(
                "Prepare",
                prepareChannel,
                preparePipe.Send,
                ReplicationMetrics.PrepareChannelSize
            );

            var writerTask = CreateChannelShovel(
                "Writer",
                sinkChannel,
                sinkPipe.Send,
                ReplicationMetrics.SinkChannelSize
            );
            var reporter = Task.Run(Report, stoppingToken);

            while (true) {
                ReplicationStatus.Start();

                await readerPipe.Start(linkedCts.Token);

                ReplicationStatus.Stop();

                while (prepareChannel.Reader.Count > 0 || sinkChannel.Reader.Count > 0) {
                    Log.Info("Waiting for the sink pipe to exhaust...");
                    await Task.Delay(1000, stoppingToken);
                }

                await checkpointStore.Flush(CancellationToken.None);

                if (linkedCts.IsCancellationRequested || !restartWhenComplete) break;

                Log.Info("Will restart in 5 sec");
                await Task.Delay(5000, stoppingToken);
            }

            try {
                await prepareTask;
                await writerTask;
                await reporter;
            }
            catch (OperationCanceledException) { }

            Task CreateChannelShovel<T>(string name, Channel<T> channel, Func<T, Task> send, IGaugeMetric size)
                => Task.Run(
                    () => channel.Shovel(
                        send,
                        () => Log.Info($"{name} started"),
                        () => Log.Info($"{name} stopped"),
                        size,
                        linkedCts.Token
                    ),
                    linkedCts.Token
                );

            async Task Report() {
                while (!stoppingToken.IsCancellationRequested) {
                    var position = await reader.GetLastPosition(stoppingToken);
                    ReplicationMetrics.LastSourcePosition.Set(position);
                    await Task.Delay(5000, stoppingToken);
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
                    await channel.Reader.WaitToReadAsync(token)) {
                    await foreach (var ctx in channel.Reader.ReadAllAsync(token)) {
                        await send(ctx);
                        channelSizeGauge.Set(channel.Reader.Count);
                    }
                }
            }
            catch (TaskCanceledException) {
                // it's ok
            }

            afterStop();
        }
    }
}