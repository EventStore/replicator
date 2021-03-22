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

        readonly IProducer<string, byte[]> _producer;

        public KafkaWriter(ProducerConfig config) {
            var producerBuilder = new ProducerBuilder<string, byte[]>(config);
            _producer = producerBuilder.Build();
        }

        public Task<long> WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken) {
            if (Log.IsDebugEnabled())
                Log.Debug(
                    "TCP: Write event with id {Id} of type {Type} to {Stream} with original position {Position}",
                    proposedEvent.EventDetails.EventId,
                    proposedEvent.EventDetails.EventType,
                    proposedEvent.EventDetails.Stream,
                    proposedEvent.SourcePosition.EventPosition
                );

            var task = proposedEvent switch {
                ProposedEvent p      => Append(p),
                ProposedMetaEvent    => NoOp(),
                ProposedDeleteStream => NoOp(),
                IgnoredEvent         => NoOp(),
                _                    => throw new InvalidOperationException("Unknown proposed event type")
            };

            return Metrics.Measure(() => task, ReplicationMetrics.WritesHistogram, ReplicationMetrics.WriteErrorsCount);

            async Task<long> Append(ProposedEvent p) {
                // TODO: Map meta to headers, but only for JSON
                // Would the byte array payload work?
                var message = new Message<string, byte[]> { };
                var result  = await _producer.ProduceAsync(p.EventDetails.Stream, message, cancellationToken);
                return result.Offset.Value;
            }

            static Task<long> NoOp() => Task.FromResult(-1L);
        }
    }
}