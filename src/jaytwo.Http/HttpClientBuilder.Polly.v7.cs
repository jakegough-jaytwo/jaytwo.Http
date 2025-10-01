#if !NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Net.Http;
using jaytwo.Http.Handlers;
using jaytwo.Http.Handlers.Polly;
using Polly;

namespace jaytwo.Http;

public partial class HttpClientBuilder
{
    private readonly List<Action<PolicyBuilder<HttpResponseMessage>>> _pollyConfigurations = new List<Action<PolicyBuilder<HttpResponseMessage>>>();

    public IHttpClientMiddleware BuildPollyMiddleware()
        => new PollyHttpClientMiddleware(config => _pollyConfigurations.ForEach(x => x(config)));

    public HttpClientBuilder ConfigurePolly(Action<PolicyBuilder<HttpResponseMessage>> config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _pollyConfigurations.Add(config);
        return this;
    }
}
#endif
