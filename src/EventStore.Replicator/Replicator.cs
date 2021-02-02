using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Prepare;
using EventStore.Replicator.Read;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;

namespace EventStore.Replicator {
    public class Replicator {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        const int Capacity = 10240;

        public async Task Replicate(
            IEventReader      reader,
            IEventWriter      writer,
            ICheckpointStore  checkpointStore,
            CancellationToken stoppingToken,
            FilterEvent?      filter    = null,
            TransformEvent?   transform = null
        ) {
            var cts = new CancellationTokenSource();

            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(stoppingToken, cts.Token);
            var start = await checkpointStore.LoadCheckpoint(stoppingToken);

            var prepareChannel = Channel.CreateBounded<PrepareContext>(Capacity);
            var sinkChannel    = Channel.CreateBounded<SinkContext>(Capacity);

            var readerPipe = new ReaderPipe(reader, start, prepareChannel.Writer);

            var preparePipe = new PreparePipe(
                filter,
                transform,
                ctx => sinkChannel.Writer.WriteAsync(ctx, ctx.CancellationToken)
            );
            var sinkPipe = new SinkPipe(writer, checkpointStore);

            var prepareTask = CreateChannelShovel("Prepare", prepareChannel, preparePipe.Send);
            var writerTask  = CreateChannelShovel("Writer", sinkChannel, sinkPipe.Send);

            await readerPipe.Start(linkedCts.Token);

            try {
                await prepareTask;
                await writerTask;
            }
            catch (OperationCanceledException) { }

            Task CreateChannelShovel<T>(string name, Channel<T> channel, Func<T, Task> send)
                => Task.Run(
                    () => channel.Shovel(
                        send,
                        () => Log.Info($"{name} started"),
                        () => Log.Info($"{name} stopped"),
                        linkedCts.Token
                    ),
                    linkedCts.Token
                );
        }
    }

    static class ChannelExtensions {
        public static async Task Shovel<T>(
            this Channel<T>   channel, Func<T, Task> send, Action beforeStart, Action afterStop,
            CancellationToken token
        ) {
            beforeStart();

            try {
                while (!token.IsCancellationRequested &&
                    await channel.Reader.WaitToReadAsync(token)) {
                    await foreach (var proposed in channel.Reader.ReadAllAsync(token)) {
                        await send(proposed);
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
