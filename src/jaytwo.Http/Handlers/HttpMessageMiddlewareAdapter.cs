using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers;

public class HttpMessageMiddlewareAdapter : DelegatingHandler
{
    private readonly Func<IHttpClientMiddleware> _middlewareFactory;

    public HttpMessageMiddlewareAdapter(HttpMessageHandler innerHandler, IHttpClientMiddleware middleware)
        : this(innerHandler, () => middleware)
    {
    }

    public HttpMessageMiddlewareAdapter(HttpMessageHandler innerHandler, Func<IHttpClientMiddleware> middlewareFactory)
        : base(innerHandler)
    {
        _middlewareFactory = middlewareFactory;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var middleware = _middlewareFactory.Invoke();

        return await middleware.SendAsync(
            request,
            cancellationToken,
            next: base.SendAsync).ConfigureAwait(false);
    }
}
