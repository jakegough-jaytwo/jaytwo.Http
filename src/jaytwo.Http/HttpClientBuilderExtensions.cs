using System;
using jaytwo.Http.Handlers.Logging;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http;

public static class HttpClientBuilderExtensions
{
    public static HttpClientBuilder WithLogger(this HttpClientBuilder builder, ILogger logger)
    {
        if (builder == null)
        {
            throw new ArgumentNullException(nameof(builder));
        }

        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        return builder.WithDelegatingHandler(() => new LoggingDelegatingHandler(logger));
    }
}
