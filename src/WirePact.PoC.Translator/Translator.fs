namespace WirePact.PoC.Translator

open Envoy.Config.Core.V3
open Envoy.Service.Auth.V3
open Envoy.Type.V3
open FSharp.Control.Tasks
open Google.Rpc
open Microsoft.Extensions.Logging

type Translator(logger: ILogger<Translator>, reader: CredentialReader, validator: ZitadelValidator) =
    inherit Authorization.AuthorizationBase()

    let fail (code: Code) status message =
        CheckResponse
            (Status = Status(Code = int code),
             DeniedResponse = DeniedHttpResponse(Status = HttpStatus(Code = status), Body = message))

    let success basicCredentials =
        let okResponse = OkHttpResponse()

        okResponse.Headers.Add
            (HeaderValueOption(Header = HeaderValue(Key = "Authorization", Value = $"Basic {basicCredentials}")))

        CheckResponse(Status = Status(Code = int Code.Ok), OkResponse = okResponse)

    override _.Check(request, _) =
        task {
            logger.LogDebug
                $"Check Request To: {request.Attributes.Request.Http.Host} {request.Attributes.Request.Http.Path}"

            let (hasHeader, authHeader) =
                request.Attributes.Request.Http.Headers.TryGetValue "authorization"

            if not hasHeader then
                logger.LogWarning
                    ("No Authorization Header provided for request to: {host}{path}",
                     request.Attributes.Request.Http.Host,
                     request.Attributes.Request.Http.Path)

                return fail Code.FailedPrecondition StatusCode.Unauthorized "No Authorization Header provided."
            else
                let! validToken = validator.IsValidToken(authHeader.Replace("Bearer ", ""))

                if validToken then
                    return success reader.BasicAuthCredentials
                else
                    logger.LogWarning
                        ("Zitadel rejected the token for request to: {host}{path}",
                         request.Attributes.Request.Http.Host,
                         request.Attributes.Request.Http.Path)

                    return fail Code.Unauthenticated StatusCode.Forbidden "No valid Authorization provided"
        }
