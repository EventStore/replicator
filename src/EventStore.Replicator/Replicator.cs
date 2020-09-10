using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Channels;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Logging;
using Open.ChannelExtensions;
using Polly;

namespace EventStore.Replicator {
    public class Replicator {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        const int Capacity = 10240;

        public async Task Replicate(
            IEventReader reader, IEventWriter writer, ICheckpointStore checkpointStore,
            CancellationToken cancellationToken,
            Func<EventRead, bool> filter,
            Func<EventRead, ValueTask<EventWrite>> transform
        ) {
            var cts       = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);
            var start     = await checkpointStore.LoadCheckpoint();

            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .RetryForeverAsync(ex => Log.Warn(ex, "Writer error: {Error}, retrying", ex.Message));

            var channel = Channel.CreateBounded<EventRead>(Capacity)
                .Source(reader.ReadEvents(start, linkedCts.Token));

            if (filter != null) channel = channel.Filter(x => Try(x, filter));

            Func<EventRead, ValueTask<EventWrite>> transformFunction;

            if (transform != null)
                transformFunction = TryTransform;
            else
                transformFunction = DefaultTransform;

            var resultChannel = channel
                .PipeAsync(5, transformFunction, Capacity, false, linkedCts.Token)
                .PipeAsync(WriteEvent, Capacity, true, linkedCts.Token)
                .Batch(1024, true, true);

            var lastPosition = new Position();

            try {
                await resultChannel.ReadUntilCancelledAsync(linkedCts.Token, StoreCheckpoint, true);
            }
            catch (Exception e) {
                Log.Fatal(e, "Unable to proceed: {Error}", e.Message);
            }
            finally {
                Log.Info("Last recorded position: {Position}", lastPosition);
            }

            async ValueTask<Position> WriteEvent(EventWrite write) {
                await retryPolicy.ExecuteAsync(() => writer.WriteEvent(write));
                return write.SourcePosition;
            }

            T Try<T>(EventRead evt, Func<EventRead, T> func) {
                try {
                    return func(evt);
                }
                catch (Exception e) {
                    Log.Error(e, "Error in the pipeline: {Error}", e.Message);
                    cts.Cancel();
                    throw;
                }
            }

            async ValueTask<EventWrite> TryTransform(EventRead evt) => await Try(evt, transform);

            ValueTask StoreCheckpoint(List<Position> positions) {
                lastPosition = positions.Last();
                return checkpointStore.StoreCheckpoint(lastPosition);
            }
        }

        static ValueTask<EventWrite> DefaultTransform(EventRead evt)
            => new ValueTask<EventWrite>(
                new EventWrite {
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
