using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.RequestTimeout;

public class RequestTimeoutHttpMessageMiddleware : IHttpClientMiddleware
{
    public RequestTimeoutHttpMessageMiddleware(TimeSpan? defaultTimeout = default)
        : this(() => defaultTimeout)
    {
    }

    public RequestTimeoutHttpMessageMiddleware(Func<TimeSpan?> defaultTimeoutFactory)
    {
        DefaultTimeoutFactory = defaultTimeoutFactory;
    }

    public Func<TimeSpan?> DefaultTimeoutFactory { get; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        TryGetState(request, out var option);
        var requestTimeout = option?.Timeout ?? DefaultTimeoutFactory();
        if (requestTimeout == null || requestTimeout == Timeout.InfiniteTimeSpan)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await new TimeoutHttpMessageMiddleware(requestTimeout.Value)
            .SendAsync(request, cancellationToken, next)
            .ConfigureAwait(false);
    }

    private static bool TryGetState(HttpRequestMessage request, out RequestTimeoutOption? state)
        => request.TryGetState(RequestTimeoutOption.Key, out state);
}
