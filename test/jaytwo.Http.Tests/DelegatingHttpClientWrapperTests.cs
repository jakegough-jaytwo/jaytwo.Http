using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using jaytwo.Http.Wrappers;
using Moq;
using Xunit;

namespace jaytwo.Http.Tests;

public class DelegatingHttpClientWrapperTests
{
    [Fact]
    public async Task SendAsync_with_no_cancellation_token_no_completion_option_Works()
    {
        // arrange
        using var request = new HttpRequestMessage();

        var mockHttpClient = new Mock<IHttpClient>();
        var testDelegatingWrapper = new TestDelegatingHttpClientWrapper(mockHttpClient.Object);
        var cancellationToken = CancellationToken.None;
        var completionOption = HttpCompletionOption.ResponseHeadersRead;

        mockHttpClient
            .Setup(x => x.SendAsync(request, completionOption, cancellationToken))
            .ReturnsAsync(new HttpResponseMessage())
            .Verifiable();

        // act
        using var response = await testDelegatingWrapper.SendAsync(request, completionOption, cancellationToken);

        // assert
        mockHttpClient.VerifyAll();
    }

    private class TestDelegatingHttpClientWrapper : DelegatingHttpClientWrapper
    {
        public TestDelegatingHttpClientWrapper(IHttpClient httpClient)
            : base(httpClient)
        {
        }
    }
}
