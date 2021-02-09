using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class PrepareContext : BasePipeContext, PipeContext {
        public PrepareContext(
            BaseOriginalEvent originalEvent, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            OriginalEvent = originalEvent;
        }

        public BaseOriginalEvent OriginalEvent { get; }
    }
}