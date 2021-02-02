using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class EventFilterFilter : IFilter<PrepareContext> {
        readonly FilterEvent _filter;

        public EventFilterFilter(FilterEvent filter) => _filter = filter;

        public async Task Send(PrepareContext context, IPipe<PrepareContext> next) {
            if (await _filter(context.OriginalEvent)) {
                await next.Send(context);
            }
        }

        public void Probe(ProbeContext context) { }
    }

    public class EventFilterSpecification : IPipeSpecification<PrepareContext> {
        readonly FilterEvent? _filter;

        public EventFilterSpecification(FilterEvent? filter) => _filter = filter;

        public void Apply(IPipeBuilder<PrepareContext> builder)
            => builder.AddFilter(new EventFilterFilter(_filter!));

        public IEnumerable<ValidationResult> Validate() {
            if (_filter == null)
                yield return this.Failure("validationFilterPipe", "Event filter is missing");
        }
    }

    public static class EventFilterPipeExtensions {
        public static void UseEventFilter(
            this IPipeConfigurator<PrepareContext> configurator, FilterEvent filter
        )
            => configurator.AddPipeSpecification(new EventFilterSpecification(filter));
    }
}
