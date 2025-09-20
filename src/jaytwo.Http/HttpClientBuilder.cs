using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;

namespace jaytwo.Http;

public class HttpClientBuilder
{
#if NET5_0_OR_GREATER
    private readonly List<Action<SocketsHttpHandler>> _handlerConfigurations = new List<Action<SocketsHttpHandler>>();
#else
    private readonly List<Action<HttpClientHandler>> _handlerConfigurations = new List<Action<HttpClientHandler>>();
#endif
    private readonly List<Func<DelegatingHandler>> _delegatingHandlerFactories = new List<Func<DelegatingHandler>>();
    private readonly List<Action<HttpClient>> _clientConfigurations = new List<Action<HttpClient>>();

    private Func<HttpMessageHandler>? _primaryHandlerFactory;

    public HttpClientBuilder()
    {
    }

    public static HttpClient Build(Action<HttpClientBuilder> clientBuilder)
    {
        var httpClientBuilder = new HttpClientBuilder();
        clientBuilder.Invoke(httpClientBuilder);
        return httpClientBuilder.Build();
    }

    public HttpClient Build(bool disposeHandler = true)
    {
        var handlerPipeline = BuildHandlerPipeline();
        var httpClient = new HttpClient(handlerPipeline, disposeHandler);
        _clientConfigurations.ForEach(x => x(httpClient));

        return httpClient;
    }

    public HttpClientBuilder WithPrimaryHandler(Func<HttpMessageHandler> handlerFactory)
    {
        if (handlerFactory == null)
        {
            throw new ArgumentNullException(nameof(handlerFactory));
        }

        _primaryHandlerFactory = handlerFactory;
        return this;
    }

    public HttpClientBuilder WithDelegatingHandler(Func<DelegatingHandler> handlerFactory)
    {
        if (handlerFactory == null)
        {
            throw new ArgumentNullException(nameof(handlerFactory));
        }

        _delegatingHandlerFactories.Add(handlerFactory);
        return this;
    }

    public HttpClientBuilder WithBaseAddress(string baseAddress)
        => WithBaseAddress(new Uri(baseAddress, UriKind.Absolute));

    public HttpClientBuilder WithBaseAddress(Uri baseAddress)
    {
        if (baseAddress == null)
        {
            throw new ArgumentNullException(nameof(baseAddress));
        }

        if (!baseAddress.IsAbsoluteUri)
        {
            throw new ArgumentException("BaseAddress must be absolute.", nameof(baseAddress));
        }

        return ConfigureClient(client => client.BaseAddress = baseAddress);
    }

    public HttpClientBuilder WithTimeout(TimeSpan timeout)
        => ConfigureClient(client => client.Timeout = timeout);

    public HttpClientBuilder WithProxy(string host, int port)
        => WithProxy(new WebProxy(host, port));

    public HttpClientBuilder WithProxy(string host, int port, string username, string password)
        => WithProxy(new WebProxy(host, port) { Credentials = new NetworkCredential(username, password) });

    public HttpClientBuilder WithProxy(IWebProxy? proxy)
        => ConfigureHandler(handler =>
        {
            handler.Proxy = proxy;
            handler.UseProxy = proxy != null;
        });

    public HttpClientBuilder WithNewCookieContainer(out CookieContainer cookieContainer)
    {
        cookieContainer = new CookieContainer();
        return WithCookieContainer(cookieContainer);
    }

    public HttpClientBuilder WithCookieContainer(CookieContainer? cookieContainer)
        => ConfigureHandler(handler => handler.CookieContainer = cookieContainer);

    public HttpClientBuilder WithUseCookies(bool useCookies)
        => ConfigureHandler(h => h.UseCookies = useCookies);

    public HttpClientBuilder WithAllowAutoRedirect(bool allowAutoRedirect)
        => ConfigureHandler(handler => handler.AllowAutoRedirect = allowAutoRedirect);

    public HttpClientBuilder WithAllowAutoRedirect(bool allowAutoRedirect, int maxAutomaticRedirections)
        => ConfigureHandler(handler =>
        {
            handler.AllowAutoRedirect = allowAutoRedirect;
            handler.MaxAutomaticRedirections = maxAutomaticRedirections;
        });

    public HttpClientBuilder WithAutomaticDecompression(DecompressionMethods automaticDecompression)
        => ConfigureHandler(handler => handler.AutomaticDecompression = automaticDecompression);

    public HttpMessageHandler BuildHandlerPipeline()
    {
        var primaryHandler = BuildPrimaryHandler();

        HttpMessageHandler result = primaryHandler;
        foreach (var handlerFactory in _delegatingHandlerFactories.AsEnumerable().Reverse())
        {
            var outerHandler = handlerFactory.Invoke();
            outerHandler.InnerHandler = result;
            result = outerHandler;
        }

        return result;
    }

    public HttpMessageHandler BuildPrimaryHandler()
    {
        if (_primaryHandlerFactory != null)
        {
            if (_handlerConfigurations.Any())
            {
                throw new InvalidOperationException("Cannot configure base handler when a custom base handler has been provided.");
            }

            return _primaryHandlerFactory.Invoke();
        }

        var baseHandler = CreateDefaultPrimaryHandler();
        foreach (var configuration in _handlerConfigurations)
        {
            configuration(baseHandler);
        }

        return baseHandler;
    }

#if NET5_0_OR_GREATER
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

#else
    public HttpClientBuilder WithRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return ConfigureHandler(handler =>
            handler.ServerCertificateCustomValidationCallback =
                (req, cert, chain, errors) => callback(req, cert, chain, errors));
    }
#endif

    public HttpClientBuilder ConfigureClient(Action<HttpClient> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _clientConfigurations.Add(configuration);
        return this;
    }

#if NET5_0_OR_GREATER
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
#else
    public HttpClientBuilder ConfigureHandler(Action<HttpClientHandler> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _handlerConfigurations.Add(configuration);
        return this;
    }

    private static HttpClientHandler CreateDefaultPrimaryHandler()
        => new HttpClientHandler();
#endif
}
