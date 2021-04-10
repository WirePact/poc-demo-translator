namespace WirePact.PoC.Translator

open System
open DotnetKubernetesClient
open Microsoft.AspNetCore.Builder
open Microsoft.AspNetCore.Hosting
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.DependencyInjection
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

type Startup(config: IConfiguration) =

    member _.ConfigureServices(services: IServiceCollection) =
        services
            .AddLogging(fun builder ->
                builder
                    .AddSimpleConsole()
                    .AddConfiguration(config)
                |> ignore)
            .AddSingleton<ZitadelValidator>()
            .AddSingleton<CredentialReader>()
            .AddGrpc(fun options ->
                options.EnableDetailedErrors <- true
                options.Interceptors.Add<TimeLogger>())
        |> ignore

#if DEBUG
        services.AddTransient<IKubernetesClient>(fun _ -> Mocks.kubernetesClient)
        |> ignore

        Environment.SetEnvironmentVariable("CREDENTIALS_SECRET_NAME", "secret")
#else
        services.AddTransient<IKubernetesClient, KubernetesClient>()
        |> ignore
#endif

        ()

    member _.Configure(app: IApplicationBuilder, env: IWebHostEnvironment) =
        if env.IsDevelopment() then
            app.UseDeveloperExceptionPage() |> ignore

        app
            .UseRouting()
            .UseEndpoints(fun endpoints -> endpoints.MapGrpcService<Translator>() |> ignore)
        |> ignore
