using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Authentication;

namespace jaytwo.Http.Handlers.Authentication;

public class RequestAuthenticationHttpMessageMiddleware : IHttpClientMiddleware
{
    public RequestAuthenticationHttpMessageMiddleware(IAuthenticationProvider? authenticationProvider = default)
        : this(() => authenticationProvider)
    {
    }

    public RequestAuthenticationHttpMessageMiddleware(Func<IAuthenticationProvider?> authenticationProviderFactory)
    {
        DefaultAuthenticationProviderFactory = authenticationProviderFactory;
    }

    public Func<IAuthenticationProvider?> DefaultAuthenticationProviderFactory { get; }

    public async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken,
        Func<HttpRequestMessage, CancellationToken, Task<HttpResponseMessage>> next)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        TryGetState(request, out var option);
        // TODO: what if i want to clear authentication on the request... i need some sort of "use authentication"
        var authenticationProvider = option?.AuthenticationProvider ?? DefaultAuthenticationProviderFactory();
        if (authenticationProvider == null)
        {
            return await next(request, cancellationToken).ConfigureAwait(false);
        }

        return await new AuthenticationHttpClientMiddleware(authenticationProvider)
            .SendAsync(request, cancellationToken, next)
            .ConfigureAwait(false);
    }

    private static bool TryGetState(HttpRequestMessage request, out RequestAuthenticationOption? state)
        => request.TryGetState(RequestAuthenticationOption.Key, out state);
}
