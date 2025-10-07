using System;
using System.Net.Http;

namespace jaytwo.Http;

internal class HttpClientAccessor : IHttpClientAccessor
{
    public HttpClientAccessor()
    {
    }

    public HttpClient? HttpClient { get; private set; }

    public void SetHttpClientContext(HttpClient httpClient)
    {
        if (httpClient == null)
        {
            throw new ArgumentNullException(nameof(httpClient));
        }

        if (HttpClient != null)
        {
            throw new InvalidOperationException($"The {nameof(HttpClient)} has already been set and cannot be changed.");
        }

        HttpClient = httpClient;
    }
}
