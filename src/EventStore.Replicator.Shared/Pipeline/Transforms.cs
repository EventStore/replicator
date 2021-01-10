using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<ProposedEvent> TransformEvent(OriginalEvent originalEvent, CancellationToken cancellationToken);

    public static class Transforms {
        public static ValueTask<ProposedEvent> Default(OriginalEvent originalEvent, CancellationToken _)
            => new(
                new ProposedEvent {
                    Data      = originalEvent.Data,
                    Metadata  = originalEvent.Metadata,
                    Stream    = originalEvent.EventStreamId,
                    EventType = originalEvent.EventType,
                    IsJson    = originalEvent.IsJson,
                    EventId   = originalEvent.EventId,
                }
            );
    }
}
