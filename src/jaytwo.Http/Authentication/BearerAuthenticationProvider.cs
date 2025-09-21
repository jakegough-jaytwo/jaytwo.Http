using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Authentication;

public class BearerAuthenticationProvider : AuthenticationProviderBase, IAuthenticationProvider
{
    public BearerAuthenticationProvider(string token)
        : this(() => token)
    {
    }

    public BearerAuthenticationProvider(Func<string> tokenDelegate)
        : this(x => Task.FromResult(tokenDelegate.Invoke()))
    {
    }

    public BearerAuthenticationProvider(Func<CancellationToken, Task<string>> tokenProvider)
    {
        TokenProvider = tokenProvider;
    }

    public BearerAuthenticationProvider(IBearerTokenProvider tokenProvider)
        : this(tokenProvider.GetTokenAsync)
    {
    }

    protected internal Func<CancellationToken, Task<string>> TokenProvider { get; }

    public override async Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var token = await TokenProvider.Invoke(cancellationToken);
        SetRequestAuthenticationHeader(request, "Bearer", token);
    }
}
