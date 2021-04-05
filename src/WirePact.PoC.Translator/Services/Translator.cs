using System.Threading.Tasks;
using Envoy.Config.Core.V3;
using Envoy.Service.Auth.V3;
using Envoy.Type.V3;
using Google.Rpc;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using WirePact.PoC.Translator.Kubernetes;
using StatusCode = Envoy.Type.V3.StatusCode;

namespace WirePact.PoC.Translator.Services
{
    public class Translator : Authorization.AuthorizationBase
    {
        private readonly ILogger<Translator> _logger;
        private readonly ZitadelValidator _validator;
        private readonly CredentialsReader _reader;

        public Translator(ILogger<Translator> logger, ZitadelValidator validator, CredentialsReader reader)
        {
            _logger = logger;
            _validator = validator;
            _reader = reader;
        }

        public override async Task<CheckResponse> Check(CheckRequest request, ServerCallContext context)
        {
            _logger.LogDebug(
                $"Check Request To: {request.Attributes.Request.Http.Host} {request.Attributes.Request.Http.Path}");

            if (!request.Attributes.Request.Http.Headers.TryGetValue("authorization", out var authHeader))
            {
                _logger.LogWarning(
                    "No Authorization Header provided for request to: {host}{path}",
                    request.Attributes.Request.Http.Host,
                    request.Attributes.Request.Http.Path);
                return Fail(Code.FailedPrecondition, StatusCode.Unauthorized, "No Authorization Header provided.");
            }

            if (!await _validator.IsValidToken(authHeader.Replace("Bearer ", string.Empty)))
            {
                _logger.LogWarning(
                    "Zitadel rejected the token for request to: {host}{path}",
                    request.Attributes.Request.Http.Host,
                    request.Attributes.Request.Http.Path);
                return Fail(Code.Unauthenticated, StatusCode.Forbidden, "No valid Authorization provided");
            }

            return Success(_reader.BasicAuthCredentials);
        }

        private static CheckResponse Fail(Code code, StatusCode status, string body) => new()
        {
            Status = new() { Code = (int)code },
            DeniedResponse = new()
            {
                Status = new HttpStatus { Code = status },
                Body = body,
            },
        };

        private static CheckResponse Success(string basicAuth) => new()
        {
            Status = new() { Code = (int)Code.Ok },
            OkResponse = new()
            {
                Headers =
                {
                    new HeaderValueOption
                    {
                        Header = new()
                        {
                            Key = "Authorization",
                            Value = $"Basic {basicAuth}",
                        },
                    },
                },
            },
        };
    }
}
