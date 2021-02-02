using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class PrepareContext : BasePipeContext, PipeContext {
        public PrepareContext(
            OriginalEvent originalEvent, Metadata metadata, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            OriginalEvent = originalEvent;
            Metadata      = metadata;
        }

        public OriginalEvent OriginalEvent { get; }
        public Metadata      Metadata      { get; }
    }
}
