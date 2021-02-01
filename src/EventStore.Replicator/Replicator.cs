using System;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Pipeline;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator {
    public class Replicator {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        Task _reader;
        Task _prepare;
        Task _writer;

        const int Capacity = 10240;

        public async Task Replicate(
            IEventReader      reader,
            IEventWriter      writer,
            ICheckpointStore  checkpointStore,
            CancellationToken cancellationToken,
            FilterEvent?      filter    = null,
            TransformEvent?   transform = null
        ) {
            var cts = new CancellationTokenSource();

            var linkedCts =
                CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var start = await checkpointStore.LoadCheckpoint(cancellationToken);

            var prepareChannel = Channel.CreateBounded<OriginalEvent>(Capacity);
            var sinkChannel    = Channel.CreateBounded<ProposedEvent>(Capacity);

            var preparePipe = Pipe.New<FilterContext>(
                cfg => {
                    cfg.UseEventFilter(filter       ?? Filters.EmptyFilter);
                    cfg.UseEventTransform(transform ?? Transforms.Default);

                    cfg.UseExecuteAsync(
                        async ctx => await sinkChannel.Writer.WriteAsync(
                            ctx.GetPayload<ProposedEvent>(),
                            ctx.CancellationToken
                        )
                    );

                    cfg.UseRetry(
                        r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
                    );
                    cfg.UseConcurrencyLimit(10);
                }
            );

            var sinkPipe = Pipe.New<SinkContext>(
                cfg => {
                    cfg.UseEventWriter(writer);
                    cfg.UseCheckpointStore(checkpointStore);

                    cfg.UseRetry(
                        r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100))
                    );
                    cfg.UseConcurrencyLimit(1);
                }
            );

            _prepare = Task.Run(
                () => Prepare(prepareChannel.Reader, preparePipe, linkedCts.Token),
                linkedCts.Token
            );

            _writer = Task.Run(
                () => Write(sinkChannel.Reader, sinkPipe, linkedCts.Token),
                linkedCts.Token
            );

            await Reader(reader, start, prepareChannel.Writer, linkedCts.Token);

            try {
                await _prepare;
                await _writer;
            }
            catch (OperationCanceledException) { }
        }

        static async Task Reader(
            IEventReader                 reader,
            Position                     start,
            ChannelWriter<OriginalEvent> writer,
            CancellationToken            token
        ) {
            Log.Info("Reader started");

            try {
                while (!token.IsCancellationRequested) {
                    await foreach (var read in reader.ReadEvents(start, token)) {
                        await writer.WriteAsync(read, token);
                    }
                }
            }
            catch (OperationCanceledException) {
                // it's ok
            }
            finally {
                Log.Info("Reader stopped");
            }
        }

        static async Task Prepare(
            ChannelReader<OriginalEvent> reader,
            IPipe<FilterContext>         pipe,
            CancellationToken            token
        ) {
            try {
                while (!token.IsCancellationRequested && await reader.WaitToReadAsync(token)) {
                    await foreach (var original in reader.ReadAllAsync(token)) {
                        await pipe.Send(new FilterContext(original, token));
                    }
                }
            }
            catch (OperationCanceledException) { }
        }

        static async Task Write(
            ChannelReader<ProposedEvent> channelReader,
            IPipe<SinkContext>           pipe,
            CancellationToken            token
        ) {
            Log.Info("Writer started");

            try {
                while (!token.IsCancellationRequested &&
                    await channelReader.WaitToReadAsync(token)) {
                    await foreach (var proposed in channelReader.ReadAllAsync(token)) {
                        await pipe.Send(new SinkContext(proposed, token));
                    }
                }
            }
            catch (TaskCanceledException) {
                // it's ok
            }

            Log.Info("Writer stopped");
        }
    }
}
