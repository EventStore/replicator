using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<bool> FilterEvent(BaseOriginalEvent originalEvent);

    public static class Filters {
        public static async ValueTask<bool> CombinedFilter(BaseOriginalEvent originalEvent, params FilterEvent[] filters) {
            foreach (var filter in filters) {
                if (!await filter(originalEvent)) return false;
            }

            return true;
        }

        public static ValueTask<bool> EventTypeFilter(BaseOriginalEvent originalEvent, Regex? include, Regex? exclude) {
            var pass =
                include.IsNullOrMatch(originalEvent.EventDetails.EventType) &&
                exclude.IsNullOrDoesntMatch(originalEvent.EventDetails.EventType);
            return new ValueTask<bool>(pass);
        }

        public static ValueTask<bool> StreamFilter(BaseOriginalEvent originalEvent, Regex? include, Regex? exclude) {
            var pass =
                include.IsNullOrMatch(originalEvent.EventDetails.Stream) &&
                exclude.IsNullOrDoesntMatch(originalEvent.EventDetails.Stream);
            return new ValueTask<bool>(pass);
        }
        
        public static ValueTask<bool> EmptyFilter(BaseOriginalEvent originalEvent) => new(true);
    }
}
