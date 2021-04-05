using System;
using System.Runtime;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Server.Kestrel.Core;
using Microsoft.Extensions.Hosting;
using WirePact.PoC.Translator;

static IHostBuilder CreateHostBuilder(string[] args) =>
    Host.CreateDefaultBuilder(args)
        .ConfigureWebHostDefaults(
            webBuilder => webBuilder.UseStartup<Startup>()
                .ConfigureKestrel(
                    server =>
                    {
                        if (int.TryParse(Environment.GetEnvironmentVariable("PORT"), out var port))
                        {
                            server.ListenAnyIP(port, o => o.Protocols = HttpProtocols.Http2);
                        }

                        if (int.TryParse(Environment.GetEnvironmentVariable("LOCAL_PORT"), out var localPort))
                        {
                            server.ListenLocalhost(localPort, o => o.Protocols = HttpProtocols.Http2);
                        }
                    }));

GCSettings.LatencyMode = GCLatencyMode.SustainedLowLatency;

// Start the webserver
await CreateHostBuilder(args).Build().RunAsync();
