#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Net.Http;
using jaytwo.Http.Handlers;
using jaytwo.Http.Handlers.Polly;
using Polly;

namespace jaytwo.Http;

public partial class HttpClientBuilder
{
    private readonly List<Action<ResiliencePipelineBuilder<HttpResponseMessage>>> _pollyConfigurations = new List<Action<ResiliencePipelineBuilder<HttpResponseMessage>>>();

    public IHttpClientMiddleware BuildPollyMiddleware()
        => new PollyHttpClientMiddleware(config => _pollyConfigurations.ForEach(x => x(config)));

    public HttpClientBuilder ConfigurePolly(Action<ResiliencePipelineBuilder<HttpResponseMessage>> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _pollyConfigurations.Add(configuration);
        return this;
    }
}
#endif
