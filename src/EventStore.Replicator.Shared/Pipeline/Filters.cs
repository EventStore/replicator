using System.Text.RegularExpressions;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;
using EventStore.Replicator.Shared.Logging;

namespace EventStore.Replicator.Shared.Pipeline {
    public delegate ValueTask<bool> FilterEvent(OriginalEvent originalEvent);

    public static class Filters {
        public static async ValueTask<bool> CombinedFilter(OriginalEvent originalEvent, params FilterEvent[] filters) {
            foreach (var filter in filters) {
                if (!await filter(originalEvent)) return false;
            }

            return true;
        }

        public static ValueTask<bool> EventTypeFilter(OriginalEvent originalEvent, Regex? include, Regex? exclude) {
            var pass =
                include.IsNullOrMatch(originalEvent.EventDetails.EventType) &&
                exclude.IsNullOrDoesntMatch(originalEvent.EventDetails.EventType);
            return new ValueTask<bool>(pass);
        }

        public static ValueTask<bool> StreamFilter(OriginalEvent originalEvent, Regex? include, Regex? exclude) {
            var pass =
                include.IsNullOrMatch(originalEvent.EventDetails.Stream) &&
                exclude.IsNullOrDoesntMatch(originalEvent.EventDetails.Stream);
            return new ValueTask<bool>(pass);
        }
        
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();
        
        public static ValueTask<bool> EmptyFilter(OriginalEvent originalEvent) {
            Log.Debug("Filtering event {Event}", originalEvent);
            return new ValueTask<bool>(true);
        }
    }
}
