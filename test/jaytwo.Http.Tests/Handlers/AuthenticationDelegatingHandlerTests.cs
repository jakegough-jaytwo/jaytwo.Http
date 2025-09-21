using System;
using System.Net;
using System.Threading.Tasks;
using jaytwo.Http.Handlers.Authentication;
using Xunit;

namespace jaytwo.Http.Tests.Handlers;

public class AuthenticationDelegatingHandlerTests
{
    public const string HttpBinUrl = HttpClientTests.HttpBinUrl;

    [Fact]
    public async Task AuthenticationDelegatingHandler_does_not_fail_when_no_authorization_present()
    {
        // arrange
        var user = "hello";
        var pass = "world";

        using var client = HttpClientBuilder.Build(builder => builder
            .WithBaseAddress(HttpBinUrl)
            .WithDelegatingHandler(() => new AuthenticationDelegatingHandler()));

        // act
        using var response = await client.SendAsync(
            request => request.WithUriPath("/basic-auth/{0}/{1}", user, pass));

        // assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task BasicAuth_authenticates_request()
    {
        // arrange
        var user = "hello";
        var pass = "world";

        using var client = HttpClientBuilder.Build(builder => builder
            .WithBaseAddress(HttpBinUrl)
            .WithDelegatingHandler(() => new AuthenticationDelegatingHandler()));

        // act
        using var response = await client.SendAsync(request => request
            .WithUriPath("/basic-auth/{0}/{1}", user, pass)
            .WithBasicAuthentication(user, pass));

        // assert
        var responseObject = await response
            .EnsureSuccessStatusCode()
            .AsAnonymousTypeAsync(new { authenticated = default(bool?), user = default(string?) });

        Assert.True(responseObject.authenticated);
        Assert.Equal(user, responseObject.user);
    }

    [Fact]
    public async Task BearerAuth_authenticates_request()
    {
        // arrange
        var token = "hello";

        using var client = HttpClientBuilder.Build(builder => builder
            .WithBaseAddress(HttpBinUrl)
            .WithDelegatingHandler(() => new AuthenticationDelegatingHandler()));

        // act
        using var response = await client.SendAsync(request => request
            .WithUriPath("/bearer")
            .WithBearerAuthentication(token));

        // assert
        var responseObject = await response
            .EnsureSuccessStatusCode()
            .AsAnonymousTypeAsync(new { authenticated = default(bool?), token = default(string?) });

        Assert.True(responseObject.authenticated);
        Assert.Equal(token, responseObject.token);
    }
}
