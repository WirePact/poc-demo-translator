namespace WirePact.PoC.Translator

open System.Diagnostics
open FSharp.Control.Tasks
open Grpc.Core.Interceptors
open Microsoft.Extensions.Logging

type TimeLogger(logger: ILogger<TimeLogger>) =
    inherit Interceptor()

    override _.UnaryServerHandler(request, context, continuation) =
        let resultTask =
            base.UnaryServerHandler(request, context, continuation)

        task {
            let watch = Stopwatch.StartNew()
            let! result = resultTask
            watch.Stop()
            logger.LogInformation $"Call to Authentication Check Handler took {watch.ElapsedMilliseconds}ms."

            return result
        }
