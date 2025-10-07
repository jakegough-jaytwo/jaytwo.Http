using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers;

internal class HttpClientAccessorHandler : DelegatingHandler
{
    private readonly IHttpClientAccessor? _httpClientAccessor;

    public HttpClientAccessorHandler(HttpMessageHandler innerHandler, IHttpClientAccessor? httpClientAccessor)
        : base(innerHandler)
    {
        _httpClientAccessor = httpClientAccessor;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        request.SetupContext(_httpClientAccessor?.HttpClient?.GetContext());

        return base.SendAsync(request, cancellationToken);
    }
}
