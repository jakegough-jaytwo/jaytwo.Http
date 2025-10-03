using System;
using System.Collections.Generic;
using System.Net.Http;
using jaytwo.Http.Authentication;
using jaytwo.Http.Handlers;

namespace jaytwo.Http;

internal class HttpClientContext
{
    public static readonly string Key = typeof(HttpClientContext).FullName!;

    private readonly List<Func<IHttpClientMiddleware>> _middlewareFactories = new();

    public HttpClientContext()
    {
    }

    public TimeSpan? DefaultTimeout { get; set; }

    public IAuthenticationProvider? DefaultAuthenticationProvider { get; set; }

    public static void Update(HttpClient client, Action<HttpClientContext> contextBuilder)
    {
        var context = client.GetContext();
        contextBuilder(context);
        SaveContext(client, context);
    }

    public static void SaveContext(HttpClient client, HttpClientContext context)
        => client.SetState(Key, context);

    public static HttpClientContext GetContext(HttpClient client)
        => client.GetOrAddState(Key, x => new HttpClientContext());

    public void AddMiddleware(Func<IHttpClientMiddleware> middlewareFactory)
        => _middlewareFactories.Add(middlewareFactory);
}
