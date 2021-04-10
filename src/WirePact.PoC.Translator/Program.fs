namespace WirePact.PoC.Translator

open System
open System.Collections.Generic
open System.IO
open System.Linq
open System.Runtime
open System.Threading.Tasks
open Microsoft.AspNetCore
open Microsoft.AspNetCore.Hosting
open Microsoft.AspNetCore.Server.Kestrel.Core
open Microsoft.Extensions.Configuration
open Microsoft.Extensions.Hosting
open Microsoft.Extensions.Logging

module Program =
    let private port name =
        match Environment.GetEnvironmentVariable name with
        | ""
        | null -> None
        | value ->
            match Int32.TryParse value with
            | false, _ -> None
            | true, int -> Some int

    let createHostBuilder args =
        Host
            .CreateDefaultBuilder(args)
            .ConfigureWebHostDefaults(fun webBuilder ->
                webBuilder
                    .UseStartup<Startup>()
                    .ConfigureKestrel(fun server ->
                        match port "PORT" with
                        | Some port -> server.ListenAnyIP(port, (fun o -> o.Protocols <- HttpProtocols.Http2))
                        | None -> ()

                        match port "LOCAL_PORT" with
                        | Some port -> server.ListenLocalhost(port, (fun o -> o.Protocols <- HttpProtocols.Http2))
                        | None -> ())
                |> ignore)

    [<EntryPoint>]
    let main args =
        GCSettings.LatencyMode <- GCLatencyMode.SustainedLowLatency
        createHostBuilder(args).Build().Run()

        0 // Exit code
