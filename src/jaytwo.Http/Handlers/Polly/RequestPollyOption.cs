using System;

namespace jaytwo.Http.Handlers.Polly;

internal class RequestPollyOption : IRequestOption
{
    public static readonly string Key = typeof(RequestPollyOption).FullName!;

    public RequestPollyOption(Func<IHttpClientMiddleware> middlewareFactory)
    {
        MiddlewareFactory = middlewareFactory;
    }

    public Func<IHttpClientMiddleware> MiddlewareFactory { get; }

    string IRequestOption.Key => Key;
}
