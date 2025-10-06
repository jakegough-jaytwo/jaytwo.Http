using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using jaytwo.Http.Authentication;
using jaytwo.Http.Handlers;
using jaytwo.Http.Handlers.Authentication;
using jaytwo.Http.Handlers.RequestTimeout;

namespace jaytwo.Http;

public partial class HttpClientBuilder
{
    private const bool DefaultDisposeHandler = true;

    private readonly List<Func<HttpMessageHandler, DelegatingHandler>> _delegatingHandlerFactories = new List<Func<HttpMessageHandler, DelegatingHandler>>();
    private readonly List<Action<HttpClient>> _clientConfigurations = new List<Action<HttpClient>>();
    private readonly List<Action<HttpClientContext>> _contextConfigurations = new List<Action<HttpClientContext>>();

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
                configureBuilder.Invoke(builder);
            },
            disposeHandler);

    public static HttpClient Build(Action<HttpClientBuilder> configureBuilder, bool disposeHandler = DefaultDisposeHandler)
    {
        var httpClientBuilder = new HttpClientBuilder()
            .WithMiddleware(() => new RequestAuthenticationHttpMessageMiddleware())
            .WithMiddleware(() => new RequestTimeoutHttpMessageMiddleware())
            .WithClientTimeout(Timeout.InfiniteTimeSpan) // timeout handled by middleware (and polly eventually)
            .WithDefaultTimeout(TimeSpan.FromSeconds(60))
            ;

        configureBuilder(httpClientBuilder);

        return httpClientBuilder.Build(disposeHandler);
    }

    public HttpClient Build(bool disposeHandler = DefaultDisposeHandler)
    {
        // TODO: this is kind of sneaky since we're including _some_ defaults with just the pipeline
        var handlerPipeline = BuildHandlerPipeline();
        var httpClient = new HttpClient(handlerPipeline, disposeHandler);
        _clientConfigurations.ForEach(x => x(httpClient));

        var clientContext = BuildContext();
        HttpClientContext.SaveContext(httpClient, clientContext);

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
        => WithDelegatingHandler(x => new HttpClientMiddlewareAdapter(x, middlewareFactory()));

    public HttpClientBuilder WithMiddleware(IHttpClientMiddleware middleware)
        => WithMiddleware(() => middleware);

    public HttpClientBuilder WithAuthenticationProvider(IAuthenticationProvider authenticationProvider)
        => ConfigureContext(x => x.DefaultAuthenticationProvider = authenticationProvider);

    public HttpClientBuilder WithBaseAddress(string baseAddress)
        => ConfigureClient(client => client.WithBaseAddress(baseAddress));

    public HttpClientBuilder WithBaseAddress(Uri baseAddress)
        => ConfigureClient(client => client.WithBaseAddress(baseAddress));

    public HttpClientBuilder WithClientTimeout(TimeSpan timeout)
        => ConfigureClient(client => client.WithTimeout(timeout));

    public HttpClientBuilder WithDefaultTimeout(TimeSpan timeout)
        => ConfigureContext(x => x.DefaultTimeout = timeout);

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

        foreach (var handlerFactory in _delegatingHandlerFactories.AsEnumerable().Reverse())
        {
            result = handlerFactory.Invoke(result);
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

    public HttpClientBuilder ConfigureClient(Action<HttpClient> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _clientConfigurations.Add(configuration);
        return this;
    }

    internal HttpClientContext BuildContext()
    {
        var result = new HttpClientContext();

        foreach (var config in _contextConfigurations)
        {
            config(result);
        }

        return result;
    }

    internal HttpClientBuilder ConfigureContext(Action<HttpClientContext> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _contextConfigurations.Add(configuration);
        return this;
    }
}
