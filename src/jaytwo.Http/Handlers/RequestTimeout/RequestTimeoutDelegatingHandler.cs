using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.RequestTimeout;

public class RequestTimeoutDelegatingHandler : DelegatingHandler
{
    public RequestTimeoutDelegatingHandler()
        : base()
    {
    }

    public RequestTimeoutDelegatingHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (!TryGetRequestTimeoutOption(request, out var requestTimeoutOption))
        {
            return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
        }

        using var requestCancellationTokenSource = new CancellationTokenSource(requestTimeoutOption.Timeout);
        using var linkedCancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, requestCancellationTokenSource.Token);
        return await base.SendAsync(request, linkedCancellationTokenSource.Token).ConfigureAwait(false);
    }

    private static bool TryGetRequestTimeoutOption(HttpRequestMessage request, out RequestTimeoutOption? requestTimeoutOption)
    {
#if NET5_0_OR_GREATER
        return request.Options.TryGetValue(new HttpRequestOptionsKey<RequestTimeoutOption>(RequestTimeoutOption.Key), out requestTimeoutOption);
#else
        if (request.Properties.TryGetValue(RequestTimeoutOption.Key, out var obj) && obj is RequestTimeoutOption asRequestTimeoutOption)
        {
            requestTimeoutOption = asRequestTimeoutOption;
            return true;
        }

        requestTimeoutOption = null!;
        return false;
#endif
    }
}
