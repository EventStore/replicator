using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<ProposedEvent> TransformEvent(
        OriginalEvent originalEvent, CancellationToken cancellationToken
    );

    public static class Transforms {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
        public static ValueTask<ProposedEvent> Default(
            OriginalEvent originalEvent, CancellationToken _
        ) {
            Log.Debug("Transforming event {Event}", originalEvent);
            return new ValueTask<ProposedEvent>(
                new ProposedEvent(
                    originalEvent.EventDetails,
                    originalEvent.Data,
                    originalEvent.Metadata,
                    originalEvent.Position,
                    originalEvent.SequenceNumber
                )
            );
        }
    }
}
