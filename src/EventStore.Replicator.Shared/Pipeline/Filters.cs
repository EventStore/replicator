using System.Text.RegularExpressions;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Extensions;

namespace EventStore.Replicator.Shared.Pipeline;

public delegate ValueTask<bool> FilterEvent(BaseOriginalEvent originalEvent);

public static class Filters {
    public static async ValueTask<bool> CombinedFilter(BaseOriginalEvent originalEvent, params FilterEvent[] filters) {
        foreach (var filter in filters) {
            if (!await filter(originalEvent)) return false;
        }

        return true;
    }

    public static ValueTask<bool> EmptyFilter(BaseOriginalEvent originalEvent) => new(true);

    public abstract class RegExFilter {
        readonly Func<BaseOriginalEvent, string> _getProp;
        readonly Regex?                          _include;
        readonly Regex?                          _exclude;

        protected RegExFilter(string? include, string? exclude, Func<BaseOriginalEvent, string> getProp) {
            _getProp = getProp;
            _include = include != null ? new Regex(include) : null;
            _exclude = exclude != null ? new Regex(exclude) : null;
        }

        public ValueTask<bool> Filter(BaseOriginalEvent originalEvent) {
            var propValue = _getProp(originalEvent);
            var pass      = _include.IsNullOrMatch(propValue) && _exclude.IsNullOrDoesntMatch(propValue);
            return new ValueTask<bool>(pass);
        }
    }

    public class EventTypeFilter : RegExFilter {
        public EventTypeFilter(string? include, string? exclude)
            : base(include, exclude, x => x.EventDetails.EventType) { }
    }

    public class StreamNameFilter : RegExFilter {
        public StreamNameFilter(string? include, string? exclude)
            : base(include, exclude, x => x.EventDetails.Stream) { }
    }
    
    public static ValueTask<bool> EmptyDataFilter(BaseOriginalEvent originalEvent) 
        => new(originalEvent is OriginalEvent { Data.Length: > 0 });
}