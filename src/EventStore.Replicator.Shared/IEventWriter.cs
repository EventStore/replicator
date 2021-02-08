using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Shared {
    public interface IEventWriter {
        Task WriteEvent(BaseProposedEvent proposedEvent, CancellationToken cancellationToken);
    }
}
