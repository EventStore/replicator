using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using es_replicator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using Serilog.Formatting.Compact;
using es_replicator.Settings;
using Microsoft.Extensions.Configuration;

var environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT");

var logConfig = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Information)
    .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
    .MinimumLevel.Override("Grpc", LogEventLevel.Error)
    .Enrich.FromLogContext();

logConfig = environment?.ToLower() == "development"
    ? logConfig.WriteTo.Console(outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} <s:{SourceContext}>;{NewLine}{Exception}")
    : logConfig.WriteTo.Console(new RenderedCompactJsonFormatter());
Log.Logger = logConfig.CreateLogger();

try {
    var fileInfo = FileVersionInfo.GetVersionInfo(Assembly.GetExecutingAssembly().Location);
    Log.Information("Starting replicator {Version}", fileInfo.ProductVersion);
    
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            webBuilder => {
                webBuilder.UseSerilog();
                webBuilder.UseStartup<Startup>();
            }
        )
        .ConfigureAppConfiguration(config => config
            .AddYamlFile("appsettings.yaml", false, true).AndEnvConfig())
        .Build()
        .Run();
    return 0;
}
catch (Exception ex) {
    Log.Fatal(ex, "Host terminated unexpectedly");

    while (true) {
        await Task.Delay(5000);
    }
    return 1;
}
finally {
    Log.CloseAndFlush();
}
