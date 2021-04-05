using System.Diagnostics;
using System.Threading.Tasks;
using Grpc.Core;
using Grpc.Core.Interceptors;
using Microsoft.Extensions.Logging;

namespace WirePact.PoC.Translator.Interceptors
{
    public class TimeLogger : Interceptor
    {
        private readonly ILogger<TimeLogger> _logger;

        public TimeLogger(ILogger<TimeLogger> logger)
        {
            _logger = logger;
        }

        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>(
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation)
        {
            var watch = Stopwatch.StartNew();
            var result = await base.UnaryServerHandler(request, context, continuation);
            watch.Stop();
            _logger.LogInformation($"Call to Authentication Check Handler took {watch.ElapsedMilliseconds}ms.");

            return result;
        }
    }
}
