using System;
using System.Net.Http;

namespace jaytwo.Http;

public interface IHttpClientAccessor
{
    public HttpClient? HttpClient { get; }
}
