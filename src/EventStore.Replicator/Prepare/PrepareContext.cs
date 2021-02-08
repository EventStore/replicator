using System.Threading;
using EventStore.Replicator.Shared.Contracts;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class PrepareContext : BasePipeContext, PipeContext {
        public PrepareContext(
            BaseOriginalEvent originalEvent, TracingMetadata tracingMetadata, CancellationToken cancellationToken
        )
            : base(cancellationToken) {
            OriginalEvent = originalEvent;
            TracingMetadata = tracingMetadata;
        }

        public BaseOriginalEvent OriginalEvent { get; }

        public TracingMetadata TracingMetadata { get; }
    }
}