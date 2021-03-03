using System;
using System.Threading.Channels;
using System.Threading.Tasks;
using GreenPipes;
using GreenPipes.Agents;

namespace EventStore.Replicator.Partitioning {
    public class PartitionChannel : Agent {
        readonly int                 _index;
        readonly Task                _reader;
        readonly ChannelWriter<Task> _writer;

        public PartitionChannel(int index) {
            _index   = index;
            var channel = Channel.CreateBounded<Task>(1);
            _reader = Task.Run(() => Reader(channel.Reader));
            _writer = channel.Writer;
        }

        public async Task Send<T>(T context, IPipe<T> next)
            where T : class, PipeContext {
            await _writer.WriteAsync(next.Send(context));
        }

        async Task Reader(ChannelReader<Task> reader) {
            while (!IsStopping) {
                try {
                    var task = await reader.ReadAsync(Stopping);
                    await task;
                }
                catch (OperationCanceledException) {
                    if (!IsStopping) throw;
                }
            }
        }

        protected override async Task StopAgent(StopContext context) {
            await _reader;
            await base.StopAgent(context);
        }

        public void Probe(ProbeContext context) => context.CreateScope($"partition-{_index}");
    }
}