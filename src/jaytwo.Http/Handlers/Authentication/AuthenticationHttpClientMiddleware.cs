using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Authentication;

namespace jaytwo.Http.Handlers.Authentication;

public class AuthenticationHttpClientMiddleware : IHttpClientMiddleware
{
    public AuthenticationHttpClientMiddleware(IAuthenticationProvider authenticationProvider)
    {
        AuthenticationProvider = authenticationProvider ?? throw new ArgumentNullException(nameof(authenticationProvider));
    }

    public IAuthenticationProvider AuthenticationProvider { get; }

    public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken, Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        await AuthenticationProvider.AuthenticateAsync(request, cancellationToken).ConfigureAwait(false);
        return await next(request, cancellationToken).ConfigureAwait(false);
    }
}
