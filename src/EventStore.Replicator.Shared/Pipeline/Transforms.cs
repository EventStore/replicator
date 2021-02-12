using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<ProposedEvent> TransformEvent(
        OriginalEvent originalEvent, CancellationToken cancellationToken
    );

    public static class Transforms {
        public static ValueTask<ProposedEvent> Default(
            OriginalEvent originalEvent, CancellationToken _
        ) {
            return new(
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
