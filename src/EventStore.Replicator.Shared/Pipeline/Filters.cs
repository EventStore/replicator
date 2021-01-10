using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<bool> FilterEvent(OriginalEvent originalEvent);

    public static class Filters {
        public static async ValueTask<bool> CombinedFilter(OriginalEvent originalEvent, params FilterEvent[] filters) {
            foreach (var filter in filters) {
                if (!await filter(originalEvent)) return false;
            }

            return true;
        }
        
        public static ValueTask<bool> EventTypeFilter(OriginalEvent originalEvent, Regex include, Regex exclude) {
            var pass = 
                (include == null || include.IsMatch(originalEvent.EventType)) && 
                (exclude == null || exclude.IsMatch(originalEvent.EventType));
            return new ValueTask<bool>(pass);
        }

        public static ValueTask<bool> StreamFilter(OriginalEvent originalEvent, Regex include, Regex exclude) {
            var pass =
                (include == null || include.IsMatch(originalEvent.EventStreamId)) &&
                (exclude == null || exclude.IsMatch(originalEvent.EventStreamId));
            return new ValueTask<bool>(pass);
        }
    }
}
