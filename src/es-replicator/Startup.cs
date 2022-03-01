using Confluent.Kafka;
using es_replicator.HttpApi;
using es_replicator.Settings;
using EventStore.Client;
using EventStore.ClientAPI;
using EventStore.Replicator;
using EventStore.Replicator.Esdb.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Sink;
using EventStore.Replicator.Esdb.Tcp;
using EventStore.Replicator.Kafka;
using EventStore.Replicator.Mongo;
using EventStore.Replicator.Prepare;
using Prometheus;
using Ensure = EventStore.Replicator.Shared.Ensure;
using NodePreference = EventStore.Client.NodePreference;
using Replicator = es_replicator.Settings.Replicator;

namespace es_replicator;

public static class Startup {
    public static void ConfigureServices(WebApplicationBuilder builder) {
        var services = builder.Services;
        Measurements.ConfigureMetrics(builder.Environment.EnvironmentName);

        var replicatorOptions = builder.Configuration.GetAs<Replicator>();

        var reader = ConfigureReader(
            Ensure.NotEmpty(
                replicatorOptions.Reader.ConnectionString,
                "Reader connection string"
            ),
            replicatorOptions.Reader.Protocol,
            replicatorOptions.Reader.PageSize,
            services
        );

        var sink = ConfigureSink(
            Ensure.NotEmpty(replicatorOptions.Sink.ConnectionString, "Sink connection string"),
            replicatorOptions.Sink.Protocol,
            replicatorOptions.Sink.Router,
            services
        );

        var filter = EventFilters.GetFilter(replicatorOptions, reader);

        var prepareOptions = new PreparePipelineOptions(
            filter,
            Transformers.GetTransformer(replicatorOptions),
            1,
            replicatorOptions.Transform?.BufferSize ?? 1
        );

        services.AddSingleton(prepareOptions);
        services.AddSingleton(reader);

        services.AddSingleton(
            new SinkPipeOptions(
                sink,
                replicatorOptions.Sink.PartitionCount,
                replicatorOptions.Sink.BufferSize,
                LoadFile(replicatorOptions.Sink.Partitioner, "Partitioner")
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

    public static void Configure(IApplicationBuilder app) {
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

    static IEventReader ConfigureReader(
        string connectionString, string protocol, int pageSize, IServiceCollection services
    )
        => protocol switch {
            "tcp" => new TcpEventReader(
                ConfigureEventStoreTcp(connectionString, true, services),
                pageSize
            ),
            "grpc" => new GrpcEventReader(ConfigureEventStoreGrpc(connectionString, true)),
            _      => throw new ArgumentOutOfRangeException(nameof(protocol))
        };

    static IEventWriter ConfigureSink(
        string connectionString, string protocol, string? router, IServiceCollection services
    ) {
        return protocol switch {
            "tcp" => new TcpEventWriter(
                ConfigureEventStoreTcp(connectionString, false, services)
            ),
            "grpc"  => new GrpcEventWriter(ConfigureEventStoreGrpc(connectionString, false)),
            "kafka" => new KafkaWriter(ParseKafkaConnection(), LoadFile(router, "Router")),
            _       => throw new ArgumentOutOfRangeException(nameof(protocol))
        };

        ProducerConfig ParseKafkaConnection() {
            var settings = connectionString.Split(';');

            var dict = settings
                .Select(ParsePair)
                .ToDictionary(x => x.Key, x => x.Value);
            return new ProducerConfig(dict);
        }

        static KeyValuePair<string, string> ParsePair(string s) {
            var split = s.Split('=');
            return new KeyValuePair<string, string>(split[0].Trim(), split[1].Trim());
        }
    }

    static IEventStoreConnection ConfigureEventStoreTcp(
        string connectionString, bool follower, IServiceCollection services
    ) {
        var builder = ConnectionSettings.Create()
            .UseCustomLogger(new TcpClientLogger())
            .KeepReconnecting()
            .KeepRetrying();

        if (follower)
            builder = builder.PreferFollowerNode();

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

    static string? LoadFile(string? name, string description) {
        if (name == null)
            return null;

        if (!File.Exists(name))
            throw new ArgumentException(
                $"{description} function file doesn't exist",
                nameof(name)
            );

        return File.ReadAllText(name);
    }
}