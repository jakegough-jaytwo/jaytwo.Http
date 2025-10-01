#if NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;

namespace jaytwo.Http;

public partial class HttpClientBuilder
{
    private readonly List<Action<SocketsHttpHandler>> _handlerConfigurations = new List<Action<SocketsHttpHandler>>();

    public HttpClientBuilder ConfigureSslOptions(Action<SslClientAuthenticationOptions> optionsBuilder)
        => ConfigureHandler(handler =>
        {
            var options = handler.SslOptions ?? new SslClientAuthenticationOptions();
            optionsBuilder(options);
            handler.SslOptions = options; // reassign to persist updated instance
        });

    public HttpClientBuilder WithRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return ConfigureSslOptions(o => o.RemoteCertificateValidationCallback = callback);
    }

    public HttpClientBuilder WithPooledConnectionLifetime(TimeSpan pooledConnectionLifetime)
        => ConfigureHandler(handler => handler.PooledConnectionLifetime = pooledConnectionLifetime);

    public HttpClientBuilder ConfigureHandler(Action<SocketsHttpHandler> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _handlerConfigurations.Add(configuration);
        return this;
    }

    private static SocketsHttpHandler CreateDefaultPrimaryHandler()
        => new SocketsHttpHandler();
}
#endif
