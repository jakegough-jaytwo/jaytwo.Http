using System;
using System.Diagnostics;
using System.Threading.Tasks;
using jaytwo.Http.Handlers.RequestTimeout;
using Xunit;

namespace jaytwo.Http.Tests.Handlers;

public class RequestTimeoutDelegatingHandlerTests
{
    public const string HttpBinUrl = HttpClientTests.HttpBinUrl;

    [Fact]
    public async Task RequestTimeoutDelegatingHandler_honors_request_timeout()
    {
        // arrange
        var clientTimeout = TimeSpan.FromSeconds(20);
        using var client = HttpClientBuilder.Build(builder => builder
            .WithBaseAddress(HttpBinUrl)
            .WithTimeout(clientTimeout)
            .WithDelegatingHandler(() => new RequestTimeoutDelegatingHandler()));

        var timer = Stopwatch.StartNew();

        // act & Assert
        var exception = await Assert.ThrowsAnyAsync<TaskCanceledException>(
            async () => await client.SendAsync(request => request
                .WithUriPath("/delay/2")
                .WithTimeout(TimeSpan.FromMilliseconds(1))));

        timer.Stop();
        Assert.True(timer.Elapsed < clientTimeout, "timeout did not happen quickly enough");
    }

    [Fact]
    public async Task RequestTimeoutDelegatingHandler_honors_client_timeout()
    {
        // arrange
        var clientTimeout = TimeSpan.FromMilliseconds(1);
        using var client = HttpClientBuilder.Build(builder => builder
            .WithBaseAddress(HttpBinUrl)
            .WithTimeout(clientTimeout)
            .WithDelegatingHandler(() => new RequestTimeoutDelegatingHandler()));

        var timer = Stopwatch.StartNew();

        // act & Assert
        var exception = await Assert.ThrowsAnyAsync<TaskCanceledException>(
            async () => await client.SendAsync(request => request
                .WithUriPath("/delay/2")));
    }
}
