using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator.Pipeline {
    public class FilterContext : BasePipeContext, PipeContext {
        public FilterContext(OriginalEvent originalEvent, CancellationToken cancellationToken)
            : base(cancellationToken)
            => OriginalEvent = originalEvent;

        public OriginalEvent OriginalEvent { get; }
    }

    public class EventFilterFilter : IFilter<FilterContext> {
        readonly FilterEvent _filter;

        public EventFilterFilter(FilterEvent filter) => _filter = filter;

        public async Task Send(FilterContext context, IPipe<FilterContext> next) {
            if (await _filter(context.OriginalEvent)) {
                await next.Send(context);
            }
        }

        public void Probe(ProbeContext context) { }
    }

    public class EventFilterSpecification : IPipeSpecification<FilterContext> {
        readonly FilterEvent _filter;

        public EventFilterSpecification(FilterEvent filter) => _filter = filter;

        public void Apply(IPipeBuilder<FilterContext> builder)
            => builder.AddFilter(new EventFilterFilter(_filter));

        public IEnumerable<ValidationResult> Validate() {
            if (_filter == null)
                yield return this.Failure("validationFilterPipe", "Event filter is missing");
        }
    }

    public static class EventFilterPipeExtensions {
        public static void UseEventFilter(
            this IPipeConfigurator<FilterContext> configurator, FilterEvent filter
        )
            => configurator.AddPipeSpecification(new EventFilterSpecification(filter));
    }
}
