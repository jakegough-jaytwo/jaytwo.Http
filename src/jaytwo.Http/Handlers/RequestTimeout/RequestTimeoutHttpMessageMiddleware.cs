using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.RequestTimeout;

public class RequestTimeoutHttpMessageMiddleware : IHttpClientMiddleware
{
    public RequestTimeoutHttpMessageMiddleware()
    {
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var requestContext = request.GetContext();

        var requestTimeout = requestContext.Timeout ?? requestContext.ClientContext.DefaultTimeout;
        if (requestTimeout == null || requestTimeout == Timeout.InfiniteTimeSpan)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await new TimeoutHttpMessageMiddleware(requestTimeout.Value)
            .SendAsync(request, cancellationToken, next)
            .ConfigureAwait(false);
    }
}
