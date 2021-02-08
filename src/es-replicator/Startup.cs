using System;
using System.Collections.Generic;
using System.Net;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using es_replicator.Settings;
using EventStore.Client;
using EventStore.ClientAPI;
using EventStore.Replicator.Grpc;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using EventStore.Replicator.Tcp;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Prometheus;
using Serilog;
using NodePreference = EventStore.Client.NodePreference;
using Position = EventStore.Replicator.Shared.Position;

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
            var reader            = ConfigureReader(replicatorOptions.Reader, services);
            var sink              = ConfigureSink(replicatorOptions.Sink, services);

            services.AddSingleton(reader);
            services.AddSingleton(sink);
            services.AddSingleton<ICheckpointStore, FakeCheckpointStore>();
            services.AddHostedService<ReplicatorService>();
            services.AddHostedService<MeasurementService>();

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app) {
            app.UseDeveloperExceptionPage();
            app.UseSerilogRequestLogging();
            app.UseRouting();

            app.UseEndpoints(
                endpoints => {
                    endpoints.MapControllers();
                    endpoints.MapMetrics();
                }
            );
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
                .UseConsoleLogger()
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

    class FakeReader : IEventReader {
        readonly ILogger<FakeReader> _log;

        public FakeReader(ILogger<FakeReader> log) => _log = log;

        public async IAsyncEnumerable<BaseOriginalEvent> ReadEvents(
            Position                                   fromPosition,
            [EnumeratorCancellation] CancellationToken cancellationToken
        ) {
            while (!cancellationToken.IsCancellationRequested) {
                _log.LogDebug("Read an event");

                yield return new OriginalEvent(
                    DateTimeOffset.Now,
                    new EventDetails("stream", Guid.NewGuid(), "type", "application/json"),
                    new byte[] {0},
                    new byte[] {1},
                    new Position(1, 1),
                    1
                );

                await Task.Delay(2, cancellationToken);
            }
        }
    }

    class FakeWriter : IEventWriter {
        readonly ILogger<FakeWriter> _log;

        public FakeWriter(ILogger<FakeWriter> log) => _log = log;

        public async Task WriteEvent(
            BaseProposedEvent proposedEvent, CancellationToken cancellationToken
        ) {
            _log.LogDebug("Writing event {Event}", proposedEvent);
            await Task.Delay(3, cancellationToken);
        }
    }

    class FakeCheckpointStore : ICheckpointStore {
        readonly ILogger<FakeCheckpointStore> _log;

        public FakeCheckpointStore(ILogger<FakeCheckpointStore> log) => _log = log;

        public ValueTask<Position> LoadCheckpoint(CancellationToken cancellationToken) {
            _log.LogInformation("Loading checkpoint");
            return new ValueTask<Position>(new Position(0, 0));
        }

        public ValueTask StoreCheckpoint(Position position, CancellationToken cancellationToken) {
            _log.LogDebug("Storing checkpoint {Position}", position);
            return new ValueTask();
        }
    }
}