using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.Polly;

public class RequestHttpMessageMiddleware<TMiddleware> : IHttpClientMiddleware
    where TMiddleware : IHttpClientMiddleware
{
    public RequestHttpMessageMiddleware(string key, TMiddleware? middleware = default)
        : this(key, () => middleware)
    {
    }

    public RequestHttpMessageMiddleware(string key, Func<TMiddleware?> middlewareFactory)
    {
        Key = key;
        DefaultMiddlewareFactory = middlewareFactory;
    }

    public string Key { get; }

    public Func<TMiddleware?> DefaultMiddlewareFactory { get; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var middleware = (TryGetState(request, out var middlewareFactory) && middlewareFactory != null)
            ? middlewareFactory()
            : DefaultMiddlewareFactory();

        // TODO: what if i want to clear it on the request... i don't want the default to kick in for that case
        if (middleware == null)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await middleware
            .SendAsync(request, cancellationToken, next)
            .ConfigureAwait(false);
    }

    private bool TryGetState(HttpRequestMessage request, out Func<TMiddleware>? state)
        => request.TryGetState(Key, out state);
}
