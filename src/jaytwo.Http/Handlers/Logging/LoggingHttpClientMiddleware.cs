using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http.Handlers.Logging;

public class LoggingHttpClientMiddleware : IHttpClientMiddleware
{
    public LoggingHttpClientMiddleware(ILogger logger)
        : this(new HttpClientLogWriter(logger))
    {
    }

    public LoggingHttpClientMiddleware(IHttpClientLogWriter logWriter)
    {
        LogWriter = logWriter;
    }

    private IHttpClientLogWriter LogWriter { get; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        using var loggerScope = LogWriter.BeginLoggerScope(out var _, out var shortRequestId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            LogWriter.LogRequest(shortRequestId, request);

            var response = await next(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            LogWriter.LogResponse(shortRequestId, stopwatch.Elapsed, response);

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();
            LogWriter.LogException(shortRequestId, stopwatch.Elapsed, exception);
            throw;
        }
    }
}
