using System;
using es_replicator.Settings;
using EventStore.Client;
using EventStore.ClientAPI;
using EventStore.Replicator;
using EventStore.Replicator.Esdb.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Pipeline;
using EventStore.Replicator.Sink;
using EventStore.Replicator.Esdb.Tcp;
using EventStore.Replicator.Http;
using EventStore.Replicator.JavaScript;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Prometheus;
using Ubiquitous.Metrics.Internals;
using Ensure = EventStore.Replicator.Shared.Ensure;
using Filter = es_replicator.Settings.Filter;
using NodePreference = EventStore.Client.NodePreference;
using Replicator = es_replicator.Settings.Replicator;

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

            var replicatorOptions = Configuration.GetAs<Replicator>();

            var reader = ConfigureReader(
                Ensure.NotEmpty(replicatorOptions.Reader.ConnectionString, "Reader connection string"),
                replicatorOptions.Reader.Protocol,
                replicatorOptions.Reader.PageSize,
                services
            );

            var sink = ConfigureSink(
                Ensure.NotEmpty(replicatorOptions.Sink.ConnectionString, "Sink connection string"),
                replicatorOptions.Sink.Protocol,
                services
            );

            var filter = EventFilters.GetFilter(replicatorOptions, reader);
            if (filter != null) services.AddSingleton(filter);

            services.AddSingleton(Transformers.GetTransformer(replicatorOptions));
            services.AddSingleton(reader);

            services.AddSingleton(
                new SinkPipeOptions(
                    sink,
                    replicatorOptions.Sink.PartitionCount
                )
            );

            services.AddSingleton<ICheckpointStore>(
                new FileCheckpointStore(replicatorOptions.Checkpoint.Path, 1000)
            );
            services.AddHostedService<ReplicatorService>();
            services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(5));

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

        static IEventReader ConfigureReader(string connectionString, string protocol, int pageSize, IServiceCollection services) {
            return protocol switch {
                "tcp"  => new TcpEventReader(ConfigureEventStoreTcp(connectionString, true, services), pageSize),
                "grpc" => new GrpcEventReader(ConfigureEventStoreGrpc(connectionString, true)),
                _      => throw new ArgumentOutOfRangeException(nameof(protocol))
            };
        }

        static IEventWriter ConfigureSink(string connectionString, string protocol, IServiceCollection services) {
            return protocol switch {
                "tcp"  => new TcpEventWriter(ConfigureEventStoreTcp(connectionString, false, services)),
                "grpc" => new GrpcEventWriter(ConfigureEventStoreGrpc(connectionString, false)),
                _      => throw new ArgumentOutOfRangeException(nameof(protocol))
            };
        }

        static IEventStoreConnection ConfigureEventStoreTcp(
            string connectionString, bool follower, IServiceCollection services
        ) {
            var builder = ConnectionSettings.Create()
                .UseCustomLogger(new TcpClientLogger())
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