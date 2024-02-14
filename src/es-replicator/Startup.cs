using es_replicator.HttpApi;
using es_replicator.Settings;
using EventStore.Replicator;
using EventStore.Replicator.Esdb.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Sink;
using EventStore.Replicator.Esdb.Tcp;
using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Kafka;
using EventStore.Replicator.Mongo;
using EventStore.Replicator.Prepare;
using Prometheus;
using Ensure = EventStore.Replicator.Shared.Ensure;
using Replicator = es_replicator.Settings.Replicator;

namespace es_replicator;

static class Startup {
    public static void ConfigureServices(WebApplicationBuilder builder) {
        Measurements.ConfigureMetrics(builder.Environment.EnvironmentName);

        var replicatorOptions = builder.Configuration.GetAs<Replicator>();

        var services = builder.Services;

        services.AddSingleton<Factory>();

        services.AddSingleton<IConfigurator, TcpConfigurator>(
            _ => new TcpConfigurator(replicatorOptions.Reader.PageSize)
        );
        services.AddSingleton<IConfigurator, GrpcConfigurator>();

        services.AddSingleton<IConfigurator, KafkaConfigurator>(
            _ => new KafkaConfigurator(replicatorOptions.Sink.Router)
        );

        services.AddSingleton(
            sp => sp.GetRequiredService<Factory>().GetReader(
                replicatorOptions.Reader.Protocol,
                Ensure.NotEmpty(
                    replicatorOptions.Reader.ConnectionString,
                    "Reader connection string"
                )
            )
        );

        services.AddSingleton(
            sp => sp.GetRequiredService<Factory>().GetWriter(
                replicatorOptions.Sink.Protocol,
                Ensure.NotEmpty(replicatorOptions.Sink.ConnectionString, "Sink connection string")
            )
        );

        services.AddSingleton(
            sp =>
                new PreparePipelineOptions(
                    EventFilters.GetFilter(replicatorOptions, sp.GetRequiredService<IEventReader>()),
                    Transformers.GetTransformer(replicatorOptions),
                    1,
                    replicatorOptions.Transform?.BufferSize ?? 1
                )
        );

        services.AddSingleton(
            new SinkPipeOptions(
                replicatorOptions.Sink.PartitionCount,
                replicatorOptions.Sink.BufferSize,
                FunctionLoader.LoadFile(replicatorOptions.Sink.Partitioner, "Partitioner")
            )
        );

        services.AddSingleton(
            new ReplicatorOptions(replicatorOptions.RestartOnFailure, replicatorOptions.RunContinuously)
        );

        RegisterCheckpointStore(replicatorOptions.Checkpoint, services);
        services.AddHostedService<ReplicatorService>();
        services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(5));
        services.AddSingleton<CountersKeep>();

        services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist");
        services.AddControllers();
        services.AddCors();
    }

    public static void Configure(WebApplication app) {
        app.UseDeveloperExceptionPage();

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

        app.MapControllers();
        app.MapMetrics();

        app.UseSpa(spa => spa.Options.SourcePath = "ClientApp");
    }

    static void RegisterCheckpointStore(Checkpoint settings, IServiceCollection services) {
        ICheckpointStore store = settings.Type switch {
            "file" => new FileCheckpointStore(settings.Path, settings.CheckpointAfter),
            "mongo" => new MongoCheckpointStore(
                settings.Path,
                settings.Database,
                settings.InstanceId,
                settings.CheckpointAfter
            ),
            _ => throw new ArgumentOutOfRangeException($"Unknown checkpoint store type: {settings.Type}")
        };
        services.AddSingleton(store);
    }
}