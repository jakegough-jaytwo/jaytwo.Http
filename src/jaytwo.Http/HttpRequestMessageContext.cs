using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Net.Http;
using jaytwo.Http.Authentication;
using jaytwo.Http.Handlers;

namespace jaytwo.Http;

internal class HttpRequestMessageContext
{
    public static readonly string Key = typeof(HttpRequestMessageContext).FullName!;

    private readonly List<Func<IHttpClientMiddleware>> _middlewareFactories = new();

    public HttpRequestMessageContext(HttpClientContext clientContext)
    {
        ClientContext = clientContext;
    }

    public HttpClientContext ClientContext { get; }

    public IAuthenticationProvider? AuthenticationProvider { get; set; }

    public TimeSpan? Timeout { get; set; }

    public static void Update(HttpRequestMessage request, Action<HttpRequestMessageContext> contextBuilder)
    {
        var context = GetContext(request);
        contextBuilder(context);
        Save(request, context);
    }

    public static void Save(HttpRequestMessage request, HttpRequestMessageContext context)
        => request.SetState(Key, context);

    public static bool TryLoad(HttpRequestMessage request, out HttpRequestMessageContext? result)
        => request.TryGetState(Key, out result);

    public static HttpRequestMessageContext GetContext(HttpRequestMessage request)
    {
        lock (request)
        {
            if (!TryLoad(request, out var context))
            {
                context = new HttpRequestMessageContext(clientContext: null!); // TODO: should we just throw if there's no client context?
                Save(request, context);
            }

            return context!;
        }
    }

    public ImmutableArray<Func<IHttpClientMiddleware>> GetAllMiddlewares()
    {
        var result = new List<Func<IHttpClientMiddleware>>();

        // last one added is the first one processed, so we need to reverse it
        foreach (var middlewareFactory in _middlewareFactories.ToImmutableArray().Reverse())
        {
            result.Add(middlewareFactory);
        }

        return result.ToImmutableArray();
    }

    public void AddMiddleware(Func<IHttpClientMiddleware> middlewareFactory)
        => _middlewareFactories.Add(middlewareFactory);
}
