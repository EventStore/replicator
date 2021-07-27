using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Logging;
using GreenPipes;
using GreenPipes.Agents;
using GreenPipes.Partitioning;

namespace EventStore.Replicator.Partitioning {
    public class ValuePartitioner : Supervisor, IPartitioner {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
        readonly string _id;

        readonly Dictionary<string, PartitionChannel> _partitions = new();

        int _partitionsCount;

        public ValuePartitioner() => _id = Guid.NewGuid().ToString("N");

        IPartitioner<T> IPartitioner.GetPartitioner<T>(PartitionKeyProvider<T> keyProvider)
            => new ContextPartitioner<T>(this, keyProvider);

        public void Probe(ProbeContext context) {
            var scope = context.CreateScope("partitioner");
            scope.Add("id", _id);

            foreach (var t in _partitions.Values)
                t.Probe(scope);
        }

        async Task Send<T>(string key, T context, IPipe<T> next) where T : class, PipeContext {
            var partitionKey = key.StartsWith("$") ? "$system" : key;
            if (!_partitions.ContainsKey(partitionKey)) {
                Log.Info("Adding new partition {Partition}", partitionKey);
                _partitions[partitionKey] = new PartitionChannel(_partitionsCount++);
            }
            await _partitions[partitionKey].Send(context, next);
        }

        class ContextPartitioner<TContext> : IPartitioner<TContext>
            where TContext : class, PipeContext {
            readonly PartitionKeyProvider<TContext> _keyProvider;
            readonly ValuePartitioner               _partitioner;

            public ContextPartitioner(
                ValuePartitioner partitioner, PartitionKeyProvider<TContext> keyProvider
            ) {
                _partitioner = partitioner;
                _keyProvider = keyProvider;
            }

            public Task Send(TContext context, IPipe<TContext> next) {
                var key = Encoding.UTF8.GetString(_keyProvider(context));

                if (key == null)
                    throw new InvalidOperationException("The key cannot be null");

                return _partitioner.Send(key, context, next);
            }

            public void Probe(ProbeContext context) => _partitioner.Probe(context);

            Task IAgent.Ready     => _partitioner.Ready;
            Task IAgent.Completed => _partitioner.Completed;

            CancellationToken IAgent.Stopping => _partitioner.Stopping;
            CancellationToken IAgent.Stopped  => _partitioner.Stopped;

            Task IAgent.Stop(StopContext context) => _partitioner.Stop(context);

            int ISupervisor. PeakActiveCount => _partitioner.PeakActiveCount;
            long ISupervisor.TotalCount      => _partitioner.TotalCount;

            void ISupervisor.Add(IAgent agent) => _partitioner.Add(agent);
        }
    }
}