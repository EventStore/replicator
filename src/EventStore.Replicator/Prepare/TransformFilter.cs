using System.Diagnostics;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Shared.Logging;
using EventStore.Replicator.Shared.Observe;
using EventStore.Replicator.Shared.Pipeline;
using GreenPipes;
using Ubiquitous.Metrics;

namespace EventStore.Replicator.Prepare;

public class TransformFilter : IFilter<PrepareContext> {
    static readonly ILog Log = LogProvider.GetCurrentClassLogger();

    readonly TransformEvent _transform;

    public TransformFilter(TransformEvent transform) => _transform = transform;

    public async Task Send(PrepareContext context, IPipe<PrepareContext> next) {
        var transformed = context.OriginalEvent is OriginalEvent oe
            ? await Metrics.Measure(() => Transform(oe), ReplicationMetrics.PrepareHistogram)
                .ConfigureAwait(false)
            : TransformMeta(context.OriginalEvent);

        if (transformed is NoEvent)
            return;

        context.AddOrUpdatePayload(() => transformed, _ => transformed);

        await next.Send(context).ConfigureAwait(false);

        async Task<BaseProposedEvent> Transform(OriginalEvent originalEvent) {
            using var activity = new Activity("transform");

            activity.SetParentId(
                context.OriginalEvent.TracingMetadata.TraceId,
                context.OriginalEvent.TracingMetadata.SpanId
            );
            activity.Start();

            try {
                var res = await _transform(
                        originalEvent,
                        context.CancellationToken
                    )
                    .ConfigureAwait(false);
                activity.SetStatus(ActivityStatusCode.Ok);
                return res;
            }
            catch (Exception e) {
                activity.SetStatus(ActivityStatusCode.Error, e.Message);

                Log.Error(
                    e,
                    "Failed to transform event from stream {Stream} of type {EventType}",
                    context.OriginalEvent.EventDetails.Stream,
                    context.OriginalEvent.EventDetails.EventType
                );
                throw;
            }
        }
    }

    public void Probe(ProbeContext context) => context.Add("eventTransform", _transform);

    static BaseProposedEvent TransformMeta(BaseOriginalEvent originalEvent)
        => originalEvent switch {
            StreamDeletedOriginalEvent deleted =>
                new ProposedDeleteStream(
                    deleted.EventDetails,
                    deleted.Position,
                    deleted.SequenceNumber
                ),
            StreamMetadataOriginalEvent meta =>
                new ProposedMetaEvent(
                    meta.EventDetails,
                    meta.Data,
                    meta.Position,
                    meta.SequenceNumber
                ),
            IgnoredOriginalEvent ignored => new IgnoredEvent(
                ignored.EventDetails,
                ignored.Position,
                ignored.SequenceNumber
            ),
            _ => throw new InvalidOperationException("Unknown original event type")
        };
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
        this IPipeConfigurator<PrepareContext> configurator,
        TransformEvent                         transformEvent
    )
        => configurator.AddPipeSpecification(new TransformSpecification(transformEvent));
}