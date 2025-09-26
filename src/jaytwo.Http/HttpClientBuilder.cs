using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Threading;
using jaytwo.Http.Authentication;
using jaytwo.Http.Handlers;
using jaytwo.Http.Handlers.Authentication;
#if NET5_0_OR_GREATER
using jaytwo.Http.Handlers.Polly;
#endif
using jaytwo.Http.Handlers.RequestTimeout;
#if NET5_0_OR_GREATER
using Polly;
#endif

namespace jaytwo.Http;

public class HttpClientBuilder
{
    private const bool DefaultDisposeHandler = true;

#if NET5_0_OR_GREATER
    private readonly List<Action<SocketsHttpHandler>> _handlerConfigurations = new List<Action<SocketsHttpHandler>>();
#else
    private readonly List<Action<HttpClientHandler>> _handlerConfigurations = new List<Action<HttpClientHandler>>();
#endif
    private readonly List<Func<HttpMessageHandler, DelegatingHandler>> _delegatingHandlerFactories = new List<Func<HttpMessageHandler, DelegatingHandler>>();
    private readonly List<Action<HttpClient>> _clientConfigurations = new List<Action<HttpClient>>();

#if NET5_0_OR_GREATER
    private readonly List<Action<ResiliencePipelineBuilder<HttpResponseMessage>>> _pollyConfigurations = new List<Action<ResiliencePipelineBuilder<HttpResponseMessage>>>();
#endif

    private TimeSpan? _defaultTimeout;
    private IAuthenticationProvider? _defaultAuthenticationProvider;
    private Func<HttpMessageHandler>? _primaryHandlerFactory;

    public HttpClientBuilder()
    {
    }

    public static HttpClient BuildDefault(bool disposeHandler = DefaultDisposeHandler)
        => BuildDefault(_ => { }, disposeHandler);

    public static HttpClient BuildDefault(Action<HttpClientBuilder> configureBuilder, bool disposeHandler = DefaultDisposeHandler)
        => Build(
            builder =>
            {
                builder
                    .WithClientTimeout(Timeout.InfiniteTimeSpan) // timeout handled by polly
                                                                 //.WithPolly(c => c.AddRetry(RetryPolicy.TimeoutAsync()))
                    .WithMiddleware(() => new PerRequestHttpClientMiddleware())
                    .WithMiddleware(() => new RequestTimeoutHttpMessageMiddleware(() => builder._defaultTimeout)) // TODO: this needs to get _defaultTimeout's value when the delegate fires
                    .WithDefaultTimeout(TimeSpan.FromSeconds(30))
                    ;

                configureBuilder.Invoke(builder);
            },
            disposeHandler);

    public static HttpClient Build(Action<HttpClientBuilder> configureBuilder, bool disposeHandler = DefaultDisposeHandler)
    {
        var httpClientBuilder = new HttpClientBuilder();
        configureBuilder(httpClientBuilder);
        return httpClientBuilder.Build(disposeHandler);
    }

    public HttpClient Build(bool disposeHandler = DefaultDisposeHandler)
    {
        // TODO: this is kind of sneaky since we're including _some_ defaults with just the pipeline
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

    public HttpClientBuilder WithDelegatingHandler(Func<HttpMessageHandler, DelegatingHandler> handlerFactory)
    {
        if (handlerFactory == null)
        {
            throw new ArgumentNullException(nameof(handlerFactory));
        }

        _delegatingHandlerFactories.Add(handlerFactory);
        return this;
    }

    public HttpClientBuilder WithMiddleware(Func<IHttpClientMiddleware> middlewareFactory)
        => WithDelegatingHandler(x => new HttpMessageMiddlewareAdapter(x, middlewareFactory()));

    public HttpClientBuilder WithMiddleware(IHttpClientMiddleware middleware)
        => WithMiddleware(() => middleware);

    public HttpClientBuilder WithAuthenticationProvider(IAuthenticationProvider authenticationProvider)
    {
        _defaultAuthenticationProvider = authenticationProvider;
        return this;
    }

#if NET5_0_OR_GREATER
    public HttpClientBuilder WithPolly(Action<ResiliencePipelineBuilder<HttpResponseMessage>> config)
    {
        if (config == null)
        {
            throw new ArgumentNullException(nameof(config));
        }

        _pollyConfigurations.Add(config);
        return this;
    }
#endif

    public HttpClientBuilder WithBaseAddress(string baseAddress)
        => ConfigureClient(client => client.WithBaseAddress(baseAddress));

    public HttpClientBuilder WithBaseAddress(Uri baseAddress)
        => ConfigureClient(client => client.WithBaseAddress(baseAddress));

    public HttpClientBuilder WithClientTimeout(TimeSpan timeout)
        => ConfigureClient(client => client.WithTimeout(timeout));

    public HttpClientBuilder WithDefaultTimeout(TimeSpan timeout)
    {
        _defaultTimeout = timeout;
        return this;
    }

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
        => ConfigureHandler(handler =>
        {
            handler.CookieContainer = cookieContainer;
            handler.UseCookies = cookieContainer != null;
        });

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
        HttpMessageHandler result = BuildPrimaryHandler();

#if NET5_0_OR_GREATER
        // first we do polly so if polly needs to retry, we log every retry if we have a logger wired up after
        if (_pollyConfigurations.Any())
        {
            result = new HttpMessageMiddlewareAdapter(
                result,
                () => new PollyHttpClientMiddleware(config => _pollyConfigurations.ForEach(x => x(config))));
        }
#endif

        foreach (var handlerFactory in _delegatingHandlerFactories)
        {
            result = handlerFactory.Invoke(result);
        }

        // last we do Authentication since it needs to be the final mutator
        result = new HttpMessageMiddlewareAdapter(
            result,
            () => new RequestAuthenticationHttpMessageMiddleware(() => _defaultAuthenticationProvider));

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
    public HttpClientBuilder ConfigurePolly(Action<ResiliencePipelineBuilder<HttpResponseMessage>> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _pollyConfigurations.Add(configuration);
        return this;
    }
#endif

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
