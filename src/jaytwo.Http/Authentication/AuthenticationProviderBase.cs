using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Authentication;

public abstract class AuthenticationProviderBase : IAuthenticationProvider
{
    public abstract Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken);

    protected void SetRequestAuthenticationHeader(HttpRequestMessage request, string headerValue)
    {
        request.Headers.Authorization = AuthenticationHeaderValue.Parse(headerValue);
    }

    protected void SetRequestAuthenticationHeader(HttpRequestMessage request, string scheme, string parameter)
    {
        request.Headers.Authorization = new AuthenticationHeaderValue(scheme, parameter);
    }
}
