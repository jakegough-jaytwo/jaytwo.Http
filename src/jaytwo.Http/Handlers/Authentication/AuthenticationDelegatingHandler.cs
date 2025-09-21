using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.Authentication;

public class AuthenticationDelegatingHandler : DelegatingHandler
{
    public AuthenticationDelegatingHandler()
        : base()
    {
    }

    public AuthenticationDelegatingHandler(HttpMessageHandler innerHandler)
        : base(innerHandler)
    {
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (TryGetRequestAuthenticationOption(request, out var requestAuthenticationOption))
        {
            await requestAuthenticationOption.AuthenticationProvider.AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
        }

        return await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
    }

    private static bool TryGetRequestAuthenticationOption(HttpRequestMessage request, out RequestAuthenticationOption? requestAuthenticationOption)
    {
#if NET5_0_OR_GREATER
        return request.Options.TryGetValue(new HttpRequestOptionsKey<RequestAuthenticationOption>(RequestAuthenticationOption.Key), out requestAuthenticationOption);
#else
        if (request.Properties.TryGetValue(RequestAuthenticationOption.Key, out var obj) && obj is RequestAuthenticationOption asRequestTimeoutOption)
        {
            requestAuthenticationOption = asRequestTimeoutOption;
            return true;
        }

        requestAuthenticationOption = null!;
        return false;
#endif
    }
}
