using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EventStore.Replicator.Shared.Logging;
using GreenPipes;
using GreenPipes.Configurators;

namespace EventStore.Replicator {
    public class LoggingFilter<T> : IFilter<T> where T : class, PipeContext {
        static readonly ILog Log = LogProvider.GetCurrentClassLogger();

        public async Task Send(T context, IPipe<T> next) {
            try {
                await next.Send(context);
            }
            catch (Exception e) {
                Log.Error(e, "Error occured in the {Type} pipe: {Message}", typeof(T).Name, e.Message);
                throw;
            }
        }

        public void Probe(ProbeContext context) { }
    }

    public class LoggingFilterSpecification<T> : IPipeSpecification<T> where T : class, PipeContext {
        public void Apply(IPipeBuilder<T> builder) => builder.AddFilter(new LoggingFilter<T>());

        public IEnumerable<ValidationResult> Validate() {
            yield return this.Success("Logging is good");
        }
    }

    public static class LoggingFilterExtensions {
        public static void UseLog<T>(this IPipeConfigurator<T> cfg) where T : class, PipeContext
            => cfg.AddPipeSpecification(new LoggingFilterSpecification<T>());
    }
}
