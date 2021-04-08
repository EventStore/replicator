using System;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using Ubiquitous.Metrics;

namespace EventStore.Replicator.Kafka {
    public class KafkaWriter : IEventWriter {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        readonly IProducer<string, byte[]>         _producer;
        readonly Action<string, object[]>?         _debug;
        readonly Func<ProposedEvent, MessageRoute> _route;

        public KafkaWriter(ProducerConfig config) {
            var producerBuilder = new ProducerBuilder<string, byte[]>(config);
            _producer = producerBuilder.Build();
            _debug    = Log.IsDebugEnabled() ? Log.Debug : null;
            _route    = RouteByCategory;
        }

        public KafkaWriter(ProducerConfig config, string? routingFunction) : this(config) {
            if (routingFunction != null)
                _route = new KafkaJsMessageRouter(routingFunction).Route;
        }

        public Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
            var task = proposedEvent switch {
                ProposedEvent p      => Append(p),
                ProposedMetaEvent    => NoOp(),
                ProposedDeleteStream => NoOp(),
                IgnoredEvent         => NoOp(),
                _                    => throw new InvalidOperationException("Unknown proposed event type")
            };

            return Metrics.Measure(() => task, ReplicationMetrics.WritesHistogram, ReplicationMetrics.WriteErrorsCount);

            async Task<long> Append(ProposedEvent p) {
                var route = _route(p);
                _debug?.Invoke(
                    "Kafka: Write event with id {Id} of type {Type} to {Stream} with original position {Position}",
                    new object[] {
                        proposedEvent.EventDetails.EventId,
                        proposedEvent.EventDetails.EventType,
                        route.Topic,
                        proposedEvent.SourcePosition.EventPosition
                    }
                );

                // TODO: Map meta to headers, but only for JSON
                var message = new Message<string, byte[]> {
                    Key   = route.PartitionKey,
                    Value = p.Data
                };
                var result = await _producer.ProduceAsync(route.Topic, message, cancellationToken);
                return result.Offset.Value;
            }

            static Task<long> NoOp() => Task.FromResult(-1L);
        }

        static MessageRoute RouteByCategory(ProposedEvent proposedEvent) {
            var catIndex = proposedEvent.EventDetails.Stream.IndexOf('-');

            var topic = catIndex >= 0
                ? proposedEvent.EventDetails.Stream[..catIndex]
                : proposedEvent.EventDetails.Stream;

            return new MessageRoute(topic, proposedEvent.EventDetails.Stream);
        }
    }
}