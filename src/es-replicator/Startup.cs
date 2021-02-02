using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using EventStore.Replicator.Shared;
using EventStore.Replicator.Shared.Contracts;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace es_replicator {
    public class Startup {
        public void ConfigureServices(IServiceCollection services) {
            services.AddSingleton<IEventReader, FakeReader>();
            services.AddSingleton<IEventWriter, FakeWriter>();
            services.AddSingleton<ICheckpointStore, FakeCheckpointStore>();
            services.AddHostedService<ReplicatorService>();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(
                endpoints => endpoints.MapGet(
                    "/",
                    context => context.Response.WriteAsync("Hello World!")
                )
            );
        }
    }

    class FakeReader : IEventReader {
        readonly ILogger<FakeReader> _log;

        public FakeReader(ILogger<FakeReader> log) => _log = log;

        public async IAsyncEnumerable<OriginalEvent> ReadEvents(
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
            ProposedEvent proposedEvent, CancellationToken cancellationToken
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
