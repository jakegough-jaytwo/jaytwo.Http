using System;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Xunit;
using Xunit.Abstractions;

namespace jaytwo.Http.Tests.Handlers;

public class LoggingDelegatingHandlerTests
{
    public const string HttpBinUrl = HttpClientTests.HttpBinUrl;

    private readonly ITestOutputHelper _output;
    private readonly ILogger _logger;
    private readonly HttpClient _httpClient;

    public LoggingDelegatingHandlerTests(ITestOutputHelper output)
    {
        _output = output;

        var factory = LoggerFactory.Create(x => x
            .AddXUnit(output)
            .SetMinimumLevel(LogLevel.Debug));

        _logger = factory.CreateLogger<LoggingDelegatingHandlerTests>();
        _httpClient = HttpClientBuilder.Build(x => x
            .WithBaseAddress(HttpBinUrl)
            .WithLogger(_logger));
    }

    [Fact]
    public async Task Error()
    {
        // arrange
        using var client = HttpClientBuilder.Build(x => x
            .WithBaseAddress(HttpBinUrl)
            .WithLogger(_logger)
            .WithTimeout(TimeSpan.FromMilliseconds(1)));

        // act
        await Assert.ThrowsAnyAsync<TaskCanceledException>(
            async () => await client.SendAsync(request => request
                .WithUriPath("/delay/2")));
    }

    [Fact]
    public async Task Get()
    {
        // arrange
        var client = _httpClient;

        // act
        using var response = await client.SendAsync(request => request
            .WithMethod(HttpMethod.Get)
            .WithUriPath("/get")
            .WithUriQueryParameter("hello", "world"));

        // assert
    }

    [Fact]
    public async Task Post()
    {
        // arrange
        var client = _httpClient;

        // act
        using var response = await client.SendAsync(request => request
            .WithMethod(HttpMethod.Post)
            .WithUriPath("/post")
            .WithJsonContent(new { hello = "world" }));

        // assert
    }
}
