using DotnetKubernetesClient;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WirePact.PoC.Translator.Interceptors;
using WirePact.PoC.Translator.Kubernetes;

namespace WirePact.PoC.Translator
{
    public class Startup
    {
        private readonly IConfiguration _configuration;

        public Startup(IConfiguration configuration)
        {
            _configuration = configuration;
        }

        public void ConfigureServices(IServiceCollection services) =>
            services
                .AddLogging(builder => builder.AddSimpleConsole().AddConfiguration(_configuration))
                .AddGrpc(
                    options =>
                    {
                        options.EnableDetailedErrors = true;
                        options.Interceptors.Add<TimeLogger>();
                    })
                .Services
                .AddSingleton<ZitadelValidator>()
                .AddSingleton<CredentialsReader>()
                .AddTransient<IKubernetesClient, KubernetesClient>();

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseRouting();

            app.UseEndpoints(
                endpoints => endpoints.MapGrpcService<Services.Translator>());
        }
    }
}
