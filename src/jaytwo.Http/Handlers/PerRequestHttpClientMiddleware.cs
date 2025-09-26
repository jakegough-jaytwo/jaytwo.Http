using System;
using System.Collections.Immutable;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers;

public sealed class PerRequestHttpClientMiddleware : IHttpClientMiddleware
{
    private static string PropertiesKey { get; } = typeof(PerRequestHttpClientMiddleware).FullName!;

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var middlewares = TryGetState(request, out HttpMessageMiddlewareCollection? state)
            ? state.GetAllMiddlewares()
            : ImmutableArray<IHttpClientMiddleware>.Empty;

        // If none were attached, just pass through.
        if (middlewares.IsDefaultOrEmpty)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        // Build the small pipeline: hN(... next: Inner) -> ... -> h0(... next: h1)
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> terminal =
            async (req, ct) => await next(req, ct).ConfigureAwait(false);

        // Compose in reverse so the first attached runs first.
        for (int i = middlewares.Length - 1; i >= 0; i--)
        {
            var current = middlewares[i];
            var nextCopy = terminal; // avoid modified closure on loop var
            terminal = async (req, ct) => await current.SendAsync(req, ct, nextCopy).ConfigureAwait(false);
        }

        return await terminal(request, cancellationToken).ConfigureAwait(false);
    }

    internal static void AddCustomMiddleware(HttpRequestMessage request, IHttpClientMiddleware middleware)
        => UpdateState(request, x => x.CustomMiddlewares.Add(middleware));

    internal static void SetAuthenticationMiddleware(HttpRequestMessage request, IHttpClientMiddleware middleware)
        => UpdateState(request, x => x.AuthenticationMiddleware = middleware);

    internal static void SetLoggingMiddleware(HttpRequestMessage request, IHttpClientMiddleware middleware)
        => UpdateState(request, x => x.LoggingMiddleware = middleware);

    private static void UpdateState(HttpRequestMessage request, Action<HttpMessageMiddlewareCollection> callback)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        if (callback is null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        if (!TryGetState(request, out var state))
        {
            state = new HttpMessageMiddlewareCollection();
        }

        callback.Invoke(state);

        SetState(request, state);
    }

    private static void SetState(HttpRequestMessage request, HttpMessageMiddlewareCollection state)
        => request.SetState(PropertiesKey, state);

    private static bool TryGetState(HttpRequestMessage request, out HttpMessageMiddlewareCollection? state)
        => request.TryGetState(PropertiesKey, out state);
}
