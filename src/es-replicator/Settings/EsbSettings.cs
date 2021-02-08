using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace es_replicator.Settings {
    public class EsdbSettings {
        public string ConnectionString { get; set; }

        public string Protocol { get; set; }
    }

    public class ReplicatorOptions {
        public EsdbSettings Reader { get; set; }

        public EsdbSettings Sink { get; set; }
    }

    public static class ConfigExtensions {
        public static T GetAs<T>(this IConfiguration configuration) where T : new() {
            T result = new();
            configuration.GetSection(typeof(T).Name).Bind(result);
            return result;
        }

        public static void AddOptions<T>(this IServiceCollection services, IConfiguration configuration)
            where T : class {
            services.Configure<T>(configuration.GetSection(typeof(T).Name));
        }
    }
}