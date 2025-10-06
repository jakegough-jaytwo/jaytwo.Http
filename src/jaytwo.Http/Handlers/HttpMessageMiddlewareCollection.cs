using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace jaytwo.Http.Handlers;

public class HttpMessageMiddlewareCollection
{
    public IHttpClientMiddleware? LoggingMiddleware { get; set; }

    public IHttpClientMiddleware? AuthenticationMiddleware { get; set; }

    public IList<IHttpClientMiddleware> CustomMiddlewares { get; } = new List<IHttpClientMiddleware>();

    public ImmutableArray<IHttpClientMiddleware> GetAllMiddlewares()
    {
        var result = new List<IHttpClientMiddleware>();

        if (LoggingMiddleware is not null)
        {
            result.Add(LoggingMiddleware);
        }

        result.AddRange(CustomMiddlewares);

        if (AuthenticationMiddleware is not null)
        {
            result.Add(AuthenticationMiddleware);
        }

        return result.ToImmutableArray();
    }
}
