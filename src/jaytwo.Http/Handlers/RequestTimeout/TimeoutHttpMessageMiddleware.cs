using System;
using System.Diagnostics;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Exceptions;

namespace jaytwo.Http.Handlers.RequestTimeout;

public class TimeoutHttpMessageMiddleware : IHttpClientMiddleware
{
    public TimeoutHttpMessageMiddleware(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    public TimeSpan Timeout { get; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var stopwatch = Stopwatch.StartNew();
        using var requestCancellationTokenSource = new CancellationTokenSource(Timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(
            cancellationToken,
            requestCancellationTokenSource.Token);

        try
        {
            return await next(request, linkedCancellationTokenSource.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex)
            when (requestCancellationTokenSource.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            throw new RequestTimedOutException($"Request timed out after {Timeout.TotalMilliseconds:n0} ms (elapsed {stopwatch.Elapsed.TotalMilliseconds:F0} ms)", ex);
        }
    }
}
