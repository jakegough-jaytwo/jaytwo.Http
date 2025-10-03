using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Handlers.Authentication;

public class RequestAuthenticationHttpMessageMiddleware : IHttpClientMiddleware
{
    public RequestAuthenticationHttpMessageMiddleware()
    {
    }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var context = request.GetContext();

        var authenticationProvider = context.AuthenticationProvider ?? context.ClientContext.DefaultAuthenticationProvider;
        if (authenticationProvider == null)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await new AuthenticationHttpClientMiddleware(authenticationProvider)
            .SendAsync(request, cancellationToken, next)
            .ConfigureAwait(false);
    }
}
