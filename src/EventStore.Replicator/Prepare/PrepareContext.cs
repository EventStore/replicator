using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class PrepareContext : BasePipeContext, PipeContext {
        public PrepareContext(
            BaseOriginalEvent originalEvent, Metadata metadata, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            OriginalEvent = originalEvent;
            Metadata      = metadata;
        }

        public BaseOriginalEvent OriginalEvent { get; }
        public Metadata      Metadata      { get; }
    }
}
