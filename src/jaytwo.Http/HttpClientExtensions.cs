using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Wrappers;

namespace jaytwo.Http;

public static class HttpClientExtensions
{
    public static IHttpClient Wrap(this HttpClient httpClient)
        => new HttpClientWrapper(httpClient);
}
