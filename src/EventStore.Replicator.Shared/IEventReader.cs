using System.Collections.Generic;
using System.Threading;
using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Shared {
    public interface IEventReader {
        IAsyncEnumerable<OriginalEvent> ReadEvents(Position fromPosition, CancellationToken cancellationToken);
    }
}
