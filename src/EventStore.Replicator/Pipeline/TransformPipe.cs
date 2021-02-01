using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator.Pipeline {
    public class TransformFilter : IFilter<FilterContext> {
        readonly TransformEvent _transform;

        public TransformFilter(TransformEvent transform) => _transform = transform;

        public async Task Send(FilterContext context, IPipe<FilterContext> next) {
            var transformed = await _transform(context.OriginalEvent, context.CancellationToken);
            context.AddOrUpdatePayload(() => transformed, _ => transformed);
            await next.Send(context);
        }

        public void Probe(ProbeContext context) => context.Add("eventTransform", _transform);
    }

    public class TransformSpecification : IPipeSpecification<FilterContext> {
        readonly TransformEvent _transform;

        public TransformSpecification(TransformEvent transform) => _transform = transform;

        public void Apply(IPipeBuilder<FilterContext> builder)
            => builder.AddFilter(new TransformFilter(_transform));

        public IEnumerable<ValidationResult> Validate() {
            yield return this.Success("filter");
        }
    }

    public static class TransformPipeExtensions {
        public static void UseEventTransform(
            this IPipeConfigurator<FilterContext> configurator, TransformEvent transformEvent
        )
            => configurator.AddPipeSpecification(new TransformSpecification(transformEvent));
    }
}
