using EventStore.Replicator.Shared.Contracts;

namespace EventStore.Replicator.Shared; 

public interface IEventReader {
    Task ReadEvents(Position fromPosition, Func<BaseOriginalEvent, ValueTask> next, CancellationToken cancellationToken);

    Task<long> GetLastPosition(CancellationToken cancellationToken);

    ValueTask<bool> Filter(BaseOriginalEvent originalEvent);
}