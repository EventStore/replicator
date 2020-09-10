using System;
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

        public async Task Replicate(
            IEventReader reader, IEventWriter writer,
            CancellationToken cancellationToken,
            Func<EventRead, bool> filter,
            Func<EventRead, ValueTask<EventWrite>> transform
        ) {
            var cts       = new CancellationTokenSource();
            var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, cts.Token);

            var retryPolicy = Policy
                .Handle<Exception>(ex => !(ex is OperationCanceledException))
                .RetryForeverAsync(ex => Log.Warn(ex, "Writer error: {Error}, retrying", ex.Message));

            var channel = Channel.CreateBounded<EventRead>(10240)
                .Source(reader.ReadEvents(linkedCts.Token));

            if (filter != null) channel = channel.Filter(x => WrapInTry(x, filter));

            Func<EventRead, ValueTask<EventWrite>> transformFunction;

            if (transform != null)
                transformFunction = GracefulTransform;
            else
                transformFunction = DefaultTransform;

            var resultChannel = channel.PipeAsync(transformFunction, 5, true, linkedCts.Token);

            await resultChannel.ReadUntilCancelledAsync(
                linkedCts.Token,
                async write => await retryPolicy.ExecuteAsync(() => writer.WriteEvent(write))
            );

            T WrapInTry<T>(EventRead evt, Func<EventRead, T> func) {
                try {
                    return func(evt);
                }
                catch (Exception e) {
                    Log.Error(e, "Error in the pipeline: {Error}", e.Message);
                    cts.Cancel();
                    throw;
                }
            }

            ValueTask<EventWrite> GracefulTransform(EventRead evt) => WrapInTry(evt, transform);
        }

        static ValueTask<EventWrite> DefaultTransform(EventRead evt)
            => new ValueTask<EventWrite>(
                new EventWrite {
                    Stream          = evt.EventStreamId,
                    EventId         = evt.EventId,
                    EventType       = evt.EventType,
                    IsJson          = evt.IsJson,
                    Data            = evt.Data,
                    Metadata        = evt.Metadata,
                    ExpectedVersion = evt.EventNumber
                }
            );
    }
}
