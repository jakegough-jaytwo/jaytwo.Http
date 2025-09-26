using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Authentication;
using jaytwo.Http.Handlers.Logging;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http;

public static class HttpClientBuilderExtensions
{
    public static HttpClientBuilder WithLogger(this HttpClientBuilder builder, ILogger logger)
        => builder.WithMiddleware(() => new LoggingHttpClientMiddleware(logger));

    public static HttpClientBuilder WithLogger(this HttpClientBuilder builder, IHttpClientLogWriter logger)
        => builder.WithMiddleware(() => new LoggingHttpClientMiddleware(logger));

    public static HttpClientBuilder WithBasicAuthentication(this HttpClientBuilder builder, string username, string password)
        => builder.WithAuthenticationProvider(new BasicAuthenticationProvider(username, password));

    public static HttpClientBuilder WithBearerAuthentication(this HttpClientBuilder builder, Func<string> tokenFactory)
        => builder.WithAuthenticationProvider(new BearerAuthenticationProvider(tokenFactory));

    public static HttpClientBuilder WithBearerAuthentication(this HttpClientBuilder builder, string token)
        => builder.WithAuthenticationProvider(new BearerAuthenticationProvider(token));

    public static HttpClientBuilder WithBearerAuthentication(this HttpClientBuilder builder, Func<CancellationToken, Task<string>> tokenFactory)
        => builder.WithAuthenticationProvider(new BearerAuthenticationProvider(tokenFactory));

    public static HttpClientBuilder WithBearerAuthentication(this HttpClientBuilder builder, IBearerTokenProvider tokenProvider)
        => builder.WithAuthenticationProvider(new BearerAuthenticationProvider(tokenProvider));
}
