using System;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using jaytwo.Http.Authentication;
using Moq;
using Xunit;

namespace jaytwo.Http.Tests;

public class HttpClientBuilderTests
{
    [Fact]
    public void WithBaseAddress_sets_BaseAddress()
    {
        // arrange
        var baseAddress = "http://example.com/";
        var builder = new HttpClientBuilder();

        // act
        using var client = builder.WithBaseAddress(baseAddress).Build();

        // assert
        Assert.Equal(baseAddress, client.BaseAddress!.AbsoluteUri);
    }

    [Fact]
    public void WithClientTimeout_sets_Client_Timeout()
    {
        // arrange
        var tiemout = TimeSpan.FromMinutes(72);
        var builder = new HttpClientBuilder();

        // act
        using var client = builder.WithClientTimeout(tiemout).Build();

        // assert
        Assert.Equal(tiemout, client.Timeout);
    }

    [Fact]
    public void WithProxy_sets_Proxy()
    {
        // arrange
        var proxy = new Mock<IWebProxy>();
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithProxy(proxy.Object).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Same(proxy.Object, handler!.Proxy);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithAllowAutoRedirect_sets_AllowAutoRedirect(bool useCookies)
    {
        // arrange
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithAllowAutoRedirect(useCookies).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Equal(useCookies, handler!.AllowAutoRedirect);
    }

    [Theory]
    [InlineData(true, 1)]
    [InlineData(true, 2)]
    [InlineData(false, 1)]
    public void WithAllowAutoRedirect_sets_AllowAutoRedirect_and_MaxAutomaticRedirections(bool allowAutoRedirect, int maxAutomaticRedirections)
    {
        // arrange
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithAllowAutoRedirect(allowAutoRedirect, maxAutomaticRedirections).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Equal(allowAutoRedirect, handler!.AllowAutoRedirect);
        Assert.Equal(maxAutomaticRedirections, handler!.MaxAutomaticRedirections);
    }

    [Theory]
    [InlineData(DecompressionMethods.All)]
    [InlineData(DecompressionMethods.Deflate)]
    [InlineData(DecompressionMethods.GZip)]
    [InlineData(DecompressionMethods.Brotli)]
    public void WithAutomaticDecompression_sets_AutomaticDecompression(DecompressionMethods automaticDecompression)
    {
        // arrange
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithAutomaticDecompression(automaticDecompression).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Equal(automaticDecompression, handler!.AutomaticDecompression);
    }

    [Fact]
    public void WithNewCookieContainer_sets_CookieContainer()
    {
        // arrange
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithNewCookieContainer(out var cookieJar).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Same(cookieJar, handler!.CookieContainer);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void WithUseCookies_sets_UseCookies(bool useCookies)
    {
        // arrange
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithUseCookies(useCookies).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Equal(useCookies, handler!.UseCookies);
    }

    [Fact]
    public void WithPooledConnectionLifetime_sets_PooledConnectionLifetime()
    {
        // arrange
        var pooledConnectionLifetime = TimeSpan.FromMinutes(72);
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithPooledConnectionLifetime(pooledConnectionLifetime).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Equal(pooledConnectionLifetime, handler!.PooledConnectionLifetime);
    }

    [Fact]
    public void WithRemoteCertificateValidationCallback_sets_RemoteCertificateValidationCallback()
    {
        // arrange
        var callback = new RemoteCertificateValidationCallback((a, b, c, d) => false);
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithRemoteCertificateValidationCallback(callback).BuildPrimaryHandler() as SocketsHttpHandler;

        // assert
        Assert.Same(callback, handler!.SslOptions.RemoteCertificateValidationCallback);
    }

    [Fact]
    public void WithPrimaryHandler_sets_PrimaryHandler()
    {
        // arrange
        var mockHandler = new Mock<HttpMessageHandler>();
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder.WithPrimaryHandler(() => mockHandler.Object).BuildPrimaryHandler();

        // assert
        Assert.Same(mockHandler.Object, handler);
    }

    [Fact]
    public void WithDefaultTimeout_sets_context_DefaultTimeout()
    {
        // arrange
        var tiemout = TimeSpan.FromMinutes(72);
        var builder = new HttpClientBuilder();

        // act
        using var client = builder.WithDefaultTimeout(tiemout).Build();

        // assert
        Assert.Equal(tiemout, client.GetContext().DefaultTimeout);
    }

    [Fact]
    public void WithAuthenticationProvider_sets_context_DefaultAuthenticationProvider()
    {
        // arrange
        var authenticationProvider = new Mock<IAuthenticationProvider>().Object;
        var builder = new HttpClientBuilder();

        // act
        using var client = builder.WithAuthenticationProvider(authenticationProvider).Build();

        // assert
        Assert.Same(authenticationProvider, client.GetContext().DefaultAuthenticationProvider);
    }

    [Fact(Skip = "TODO")]
    public void WithDelegatingHandler_configures_DelegatingHandlers_in_order()
    {
        // arrange
        var mockPrimaryHandler = new Mock<DelegatingHandler>();
        var mockFirstDelegatingHandler = new Mock<DelegatingHandler>();
        var mockSecondDelegatingHandler = new Mock<DelegatingHandler>();
        var builder = new HttpClientBuilder();

        // act
        using var handler = builder
            .WithPrimaryHandler(() => mockPrimaryHandler.Object)
            .WithDelegatingHandler(x => mockFirstDelegatingHandler.Object)
            .WithDelegatingHandler(x => mockSecondDelegatingHandler.Object)
            .BuildHandlerPipeline();

        // assert
        Assert.Same(mockSecondDelegatingHandler.Object.InnerHandler, mockFirstDelegatingHandler.Object);
        Assert.Same(mockFirstDelegatingHandler.Object.InnerHandler, mockPrimaryHandler.Object);
    }
}
