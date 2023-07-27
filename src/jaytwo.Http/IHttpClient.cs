using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http;

public interface IHttpClient : IDisposable
{
    Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, HttpCompletionOption? completionOption = default, CancellationToken? cancellationToken = default);
}
