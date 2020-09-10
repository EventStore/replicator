using System.Threading.Tasks;

namespace EventStore.Replicator.Shared {
    public interface IEventWriter {
        Task WriteEvent(EventWrite eventWrite);
    }
}
