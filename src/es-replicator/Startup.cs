using es_replicator.HttpApi;
using es_replicator.Settings;
using EventStore.Replicator;
using EventStore.Replicator.Esdb.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Sink;
using EventStore.Replicator.Esdb.Tcp;
using EventStore.Replicator.JavaScript;
using EventStore.Replicator.Kafka;
using EventStore.Replicator.Prepare;
using Prometheus;
using Ensure = EventStore.Replicator.Shared.Ensure;
using Replicator = es_replicator.Settings.Replicator;

namespace es_replicator; 

public class Startup {
    IConfiguration      Configuration { get; }
    IWebHostEnvironment Environment   { get; }

    public Startup(IConfiguration configuration, IWebHostEnvironment environment) {
        Configuration = configuration;
        Environment   = environment;
    }

    public void ConfigureServices(IServiceCollection services) {
        Measurements.ConfigureMetrics(Environment.EnvironmentName);

        var replicatorOptions = Configuration.GetAs<Replicator>();

        services.AddSingleton<Factory>();

        services.AddSingleton<IConfigurator, TcpConfigurator>(
            _ => new TcpConfigurator(replicatorOptions.Reader.PageSize, services)
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
            sp => new SinkPipeOptions(
                sp.GetRequiredService<IEventWriter>(),
                replicatorOptions.Sink.PartitionCount,
                replicatorOptions.Sink.BufferSize,
                FunctionLoader.LoadFile(replicatorOptions.Sink.Partitioner, "Partitioner")
            )
        );

        services.AddSingleton(
            new ReplicatorOptions(replicatorOptions.RestartOnFailure, replicatorOptions.RunContinuously)
        );

        services.AddSingleton<ICheckpointStore>(
            new FileCheckpointStore(replicatorOptions.Checkpoint.Path, 1000)
        );
        services.AddHostedService<ReplicatorService>();
        services.Configure<HostOptions>(opts => opts.ShutdownTimeout = TimeSpan.FromMinutes(5));
        services.AddSingleton<CountersKeep>();

        services.AddSpaStaticFiles(configuration => configuration.RootPath = "ClientApp/dist");
        services.AddControllers();
        services.AddCors();
    }

    public void Configure(IApplicationBuilder app) {
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

        app.UseRouting();

        app.UseEndpoints(
            endpoints => {
                endpoints.MapControllers();
                endpoints.MapMetrics();
            }
        );

        app.UseSpa(spa => spa.Options.SourcePath = "ClientApp");
    }
}