using System;
using es_replicator.Settings;
using EventStore.Client;
using EventStore.ClientAPI;
using EventStore.Replicator;
using EventStore.Replicator.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Tcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Serilog;
using NodePreference = EventStore.Client.NodePreference;

namespace es_replicator {
    public class Startup {
        IConfiguration Configuration { get; }

        IWebHostEnvironment Environment { get; }

        public Startup(IConfiguration configuration, IWebHostEnvironment environment) {
            Configuration = configuration;
            Environment   = environment;
        }

        public void ConfigureServices(IServiceCollection services) {
            Measurements.ConfigureMetrics(Environment.EnvironmentName);

            var replicatorOptions = Configuration.GetAs<ReplicatorOptions>();

            var reader = ConfigureReader(replicatorOptions.Reader, services);
            var sink   = ConfigureSink(replicatorOptions.Sink, services);

            if (replicatorOptions.Scavenge)
                services.AddSingleton<FilterEvent>(ctx => ctx.GetRequiredService<IEventReader>().Filter);
            services.AddSingleton(reader);
            services.AddSingleton(sink);

            services.AddSingleton<ICheckpointStore>(
                new FileCheckpointStore(Configuration["Checkpoint:Path"], 1000)
            );
            services.AddHostedService<ReplicatorService>();

            services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist");
            services.AddControllers();
            services.AddCors();
        }

        public void Configure(IApplicationBuilder app) {
            app.UseDeveloperExceptionPage();
            // app.UseSerilogRequestLogging();

            app.UseCors(
                cfg => {
                    cfg.AllowAnyMethod();
                    cfg.AllowAnyOrigin();
                    cfg.AllowAnyHeader();
                }
            );

            app.UseDefaultFiles();
            app.UseStaticFiles();
            app.UseSpaStaticFiles();

            app.UseRouting();

            app.UseEndpoints(
                endpoints => {
                    endpoints.MapControllers();
                    endpoints.MapMetrics();
                }
            );

            app.UseSpa(spa => spa.Options.SourcePath = "ClientApp");
        }

        static IEventReader ConfigureReader(EsdbSettings settings, IServiceCollection services) {
            return settings.Protocol switch {
                "tcp"  => new TcpEventReader(ConfigureEventStoreTcp(settings.ConnectionString, true, services)),
                "grpc" => new GrpcEventReader(ConfigureEventStoreGrpc(settings.ConnectionString, true)),
                _      => throw new ArgumentOutOfRangeException(nameof(settings.Protocol))
            };
        }

        static IEventWriter ConfigureSink(EsdbSettings settings, IServiceCollection services) {
            return settings.Protocol switch {
                "tcp"  => new TcpEventWriter(ConfigureEventStoreTcp(settings.ConnectionString, false, services)),
                "grpc" => new GrpcEventWriter(ConfigureEventStoreGrpc(settings.ConnectionString, false)),
                _      => throw new ArgumentOutOfRangeException(nameof(settings.Protocol))
            };
        }

        static IEventStoreConnection ConfigureEventStoreTcp(
            string connectionString, bool follower, IServiceCollection services
        ) {
            var builder = ConnectionSettings.Create()
                .KeepReconnecting()
                .KeepRetrying();

            if (follower) builder = builder.PreferFollowerNode();

            var connection = EventStoreConnection.Create(connectionString, builder);

            services.AddSingleton<IHostedService>(new TcpConnectionService(connection));

            return connection;
        }

        static EventStoreClient ConfigureEventStoreGrpc(string connectionString, bool follower) {
            var settings = EventStoreClientSettings.Create(connectionString);

            if (follower)
                settings.ConnectivitySettings.NodePreference = NodePreference.Follower;
            return new EventStoreClient(settings);
        }
    }
}