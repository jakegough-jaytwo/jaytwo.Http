using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using Xunit;

namespace jaytwo.Http.Tests;

public class HttpClientWrapperTests
{
    [Fact]
    public async Task SendAsync_with_no_cancellation_token_no_completion_option_Works()
    {
        // arrange
        using var request = new HttpRequestMessage() { RequestUri = new Uri("http://example.com") };
        using var response = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent("hello world"),
        };

        var mockHandler = new Mock<HttpMessageHandler>();
        mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(x => x.RequestUri == request.RequestUri),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(response)
            .Verifiable();

        using var httpClient = new HttpClient(mockHandler.Object);

        using var wrappedHttpClient = httpClient.Wrap();

        // act
        using var actualHttpResponseMessage = await wrappedHttpClient.SendAsync(request);

        // assert
        mockHandler.VerifyAll();
        Assert.Equal(HttpStatusCode.OK, actualHttpResponseMessage.StatusCode);
        Assert.Equal(response, actualHttpResponseMessage);
    }
}
