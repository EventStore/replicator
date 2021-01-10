using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Pipeline;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator {
    public class Replicator {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        Task _reader;

        const int Capacity = 10240;

        public async Task Replicate(
            IEventReader reader, IEventWriter writer, 
            ICheckpointStore checkpointStore,
            CancellationToken cancellationToken,
            Func<OriginalEvent, bool> filter,
            Func<OriginalEvent, ValueTask<ProposedEvent>> transform
        ) {
            var cts       = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var start     = await checkpointStore.LoadCheckpoint(cancellationToken);

            var prepareChannel = Channel.CreateBounded<OriginalEvent>(Capacity);

            var preparePipe = Pipe.New<FilterContext>(
                cfg => {
                    cfg.UseEventFilter(x => Filters.StreamFilter(x, null, null));
                    cfg.UseEventTransform(Transforms.Default);
                    cfg.UseExecuteAsync(async ctx => await prepareChannel.Writer.WriteAsync(ctx.OriginalEvent, ctx.CancellationToken));
                    cfg.UseRetry(r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)));
                    cfg.UseConcurrencyLimit(10);
                }
            );

            var sinkChannel = Channel.CreateBounded<ProposedEvent>(Capacity);

            var sinkPipe = Pipe.New<SinkContext>(
                cfg => {
                    cfg.UseEventWriter(writer);
                    cfg.UseRetry(r => r.Incremental(10, TimeSpan.Zero, TimeSpan.FromMilliseconds(100)));
                    cfg.UseConcurrencyLimit(1);
                }
            );

            _reader = Task.Run(() => Reader(reader, start, preparePipe, linkedCts.Token));

            // var channel = Channel.CreateBounded<EventRead>(Capacity)
            // .Source(reader.ReadEvents(start, linkedCts.Token));

            // if (filter != null) channel = channel.Filter(x => Try(x, filter));

            // var resultChannel = channel
            // .PipeAsync(5, transformFunction, Capacity, false, linkedCts.Token)
            // .PipeAsync(WriteEvent, Capacity, true, linkedCts.Token)
            // .Batch(1024, true, true);

            // var lastPosition = new Position();

            // try {
                // await resultChannel.ReadUntilCancelledAsync(linkedCts.Token, StoreCheckpoint, true);
            // }
            // catch (Exception e) {
                // Log.Fatal(e, "Unable to proceed: {Error}", e.Message);
            // }
            // finally {
                // Log.Info("Last recorded position: {Position}", lastPosition);
            // }
        }

        static async Task Reader(IEventReader reader, Position start, IPipe<FilterContext> pipe, CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    await foreach (var read in reader.ReadEvents(start, token)) {
                        await pipe.Send(new FilterContext(read, token));
                    }
                }
                catch (TaskCanceledException) {
                    // it's ok
                }
            }
        }

        static async Task Write(ChannelReader<OriginalEvent> readChannel, IPipe<SinkContext> pipe, CancellationToken token) {
            while (!token.IsCancellationRequested) {
                try {
                    await foreach (var prepared in readChannel.ReadAllAsync(token)) {
                        
                    }
                }
                catch (TaskCanceledException) {
                    // it's ok
                }
            }
        }

        static ValueTask<ProposedEvent> DefaultTransform(OriginalEvent evt)
            => new(
                new ProposedEvent {
                    Stream         = evt.EventStreamId,
                    EventId        = evt.EventId,
                    EventType      = evt.EventType,
                    IsJson         = evt.IsJson,
                    Data           = evt.Data,
                    Metadata       = evt.Metadata,
                    SourcePosition = evt.Position
                }
            );
    }
}
