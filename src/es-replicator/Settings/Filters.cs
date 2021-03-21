using System;
using System.Linq;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;
using Ubiquitous.Metrics.Internals;

namespace es_replicator.Settings {
    public static class EventFilters {
        public static FilterEvent? GetFilter(Replicator settings, IEventReader reader) {
            var filters = settings.Filters.ValueOrEmpty().Select(Configure).ToList();
            if (settings.Scavenge) filters.Add(reader.Filter);

            return filters.Count > 1
                ? x => Filters.CombinedFilter(x, filters.ToArray())
                : filters[0];

            static FilterEvent Configure(Filter cfg) => cfg.Type switch {
                "eventType"  => new Filters.EventTypeFilter(cfg.Include, cfg.Exclude).Filter,
                "streamName" => new Filters.StreamNameFilter(cfg.Include, cfg.Exclude).Filter,
                _            => throw new ArgumentException($"Unknown filter: {cfg.Type}")
            };
        }
    }
}