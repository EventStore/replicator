using System.Threading;
using System.Threading.Tasks;

namespace EventStore.Replicator.Shared {
    public interface IEventWriter {
        Task WriteEvent(ProposedEvent proposedEvent, CancellationToken cancellationToken);
    }
}
