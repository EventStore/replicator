using es_replicator;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Hosting;

Host.CreateDefaultBuilder(args)
    .ConfigureWebHostDefaults(b => b.UseStartup<Startup>())
    .Build()
    .Run();
