using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace jaytwo.Http.Tests;

public class IHttpClientExtensionsTests
{
    public const string HttpBinUrl = "http://httpbin.jaytwo.com/";

    [Fact]
    public async Task SendAsync_with_no_cancellation_token_no_completion_option_Works()
    {
        // arrange
        using var request = new HttpRequestMessage();

        var mockHttpClient = new Mock<IHttpClient>();

        mockHttpClient
            .Setup(x => x.SendAsync(request, null, null))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await mockHttpClient.Object.SendAsync(request);

        // assert
        mockHttpClient.VerifyAll();
    }

    [Fact]
    public async Task SendAsync_with_CancellationToken_Works()
    {
        // arrange
        using var request = new HttpRequestMessage();
        var cancellationToken = CancellationToken.None;

        var mockHttpClient = new Mock<IHttpClient>();

        mockHttpClient
            .Setup(x => x.SendAsync(request, null, cancellationToken))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await mockHttpClient.Object.SendAsync(request, cancellationToken);

        // assert
        mockHttpClient.VerifyAll();
    }

    [Theory]
    [InlineData(HttpCompletionOption.ResponseHeadersRead)]
    [InlineData(HttpCompletionOption.ResponseContentRead)]
    public async Task SendAsync_with_RequestBuilderAction_Works(HttpCompletionOption httpCompletionOption)
    {
        // arrange
        using var request = new HttpRequestMessage();
        var requestUri = new Uri($"http://{Guid.NewGuid()}/");
        var cancellationToken = new CancellationToken(true);

        var mockHttpClient = new Mock<IHttpClient>();

        mockHttpClient
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(x => x.RequestUri == requestUri), httpCompletionOption, cancellationToken))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await mockHttpClient.Object.SendAsync(
            request => request.RequestUri = requestUri,
            httpCompletionOption,
            cancellationToken);

        // assert
        mockHttpClient.VerifyAll();
    }

    [Fact]
    public async Task SendAsync_with_RequestBuilderAction_Works_2()
    {
        // arrange
        using var request = new HttpRequestMessage();
        var requestUri = new Uri($"http://{Guid.NewGuid()}/");
        var cancellationToken = new CancellationToken(true);

        var mockHttpClient = new Mock<IHttpClient>();

        mockHttpClient
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(x => x.RequestUri == requestUri), null, cancellationToken))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await mockHttpClient.Object.SendAsync(
            request => request.RequestUri = requestUri,
            cancellationToken);

        // assert
        mockHttpClient.VerifyAll();
    }

    [Fact]
    public async Task SendAsync_with_RequestBuilderAction_Works_3()
    {
        // arrange
        using var request = new HttpRequestMessage();
        var requestUri = new Uri($"http://{Guid.NewGuid()}/");
        var cancellationToken = new CancellationToken(true);

        var mockHttpClient = new Mock<IHttpClient>();

        mockHttpClient
            .Setup(x => x.SendAsync(It.Is<HttpRequestMessage>(x => x.RequestUri == requestUri), null, cancellationToken))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await mockHttpClient.Object.SendAsync(
            request =>
            {
                request.RequestUri = requestUri;
                return Task.CompletedTask;
            },
            cancellationToken);

        // assert
        mockHttpClient.VerifyAll();
    }
}
