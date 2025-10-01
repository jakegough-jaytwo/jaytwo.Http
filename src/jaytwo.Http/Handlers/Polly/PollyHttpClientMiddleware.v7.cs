#if !NET6_0_OR_GREATER

using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Polly;

namespace jaytwo.Http.Handlers.Polly;

public class PollyHttpClientMiddleware : IHttpClientMiddleware
{
    private readonly Func<IAsyncPolicy<HttpResponseMessage>?> _policyFactory;

    public PollyHttpClientMiddleware(Action<PolicyBuilder<HttpResponseMessage>> config)
        : this(() => throw new Exception("nope"))
    {
    }

    public PollyHttpClientMiddleware(IAsyncPolicy<HttpResponseMessage>? policy)
        : this(() => policy)
    {
    }

    public PollyHttpClientMiddleware(Func<IAsyncPolicy<HttpResponseMessage>?> policyFactory)
    {
        _policyFactory = policyFactory;
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        var policy = _policyFactory();

        if (policy == null)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await policy
            .ExecuteAsync(async token => await next(request, token), cancellationToken)
            .ConfigureAwait(false);
    }
}
#endif
