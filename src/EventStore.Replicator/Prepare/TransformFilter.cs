using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;

namespace EventStore.Replicator.Prepare {
    public class TransformFilter : IFilter<PrepareContext> {
        readonly TransformEvent _transform;

        public TransformFilter(TransformEvent transform) => _transform = transform;

        public async Task Send(PrepareContext context, IPipe<PrepareContext> next) {
            using (var activity = new Activity("transform")) {
                activity.SetParentId(context.Metadata.TraceId, context.Metadata.SpanId);
                activity.Start();

                var transformed = await _transform(
                    context.OriginalEvent,
                    context.CancellationToken
                );
                context.AddOrUpdatePayload(() => transformed, _ => transformed);
            }

            await next.Send(context);
        }

        public void Probe(ProbeContext context) => context.Add("eventTransform", _transform);
    }

    public class TransformSpecification : IPipeSpecification<PrepareContext> {
        readonly TransformEvent _transform;

        public TransformSpecification(TransformEvent transform) => _transform = transform;

        public void Apply(IPipeBuilder<PrepareContext> builder)
            => builder.AddFilter(new TransformFilter(_transform));

        public IEnumerable<ValidationResult> Validate() {
            yield return this.Success("filter");
        }
    }

    public static class TransformPipeExtensions {
        public static void UseEventTransform(
            this IPipeConfigurator<PrepareContext> configurator, TransformEvent transformEvent
        )
            => configurator.AddPipeSpecification(new TransformSpecification(transformEvent));
    }
}
