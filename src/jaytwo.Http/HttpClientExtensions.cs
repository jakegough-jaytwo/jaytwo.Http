using System;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http;

public static class HttpClientExtensions
{
    private const HttpCompletionOption DefaultHttpCompletionOption = HttpCompletionOption.ResponseContentRead;
    private const bool DefaultEnsureSuccessStatusCode = true;

    public static async Task<HttpResponseMessage> SendAsync(
        this HttpClient httpClient,
        Action<HttpRequestMessage> requestBuilder,
        CancellationToken cancellationToken)
        => await SendAsync(
            httpClient,
            requestBuilder: RequestBuilderToAsync(requestBuilder),
            cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(
        this HttpClient httpClient,
        Action<HttpRequestMessage> requestBuilder,
        HttpCompletionOption completionOption = DefaultHttpCompletionOption,
        bool ensureSuccessStatusCode = DefaultEnsureSuccessStatusCode,
        CancellationToken cancellationToken = default)
        => await SendAsync(
            httpClient,
            requestBuilder: RequestBuilderToAsync(requestBuilder),
            completionOption: completionOption,
            ensureSuccessStatusCode: ensureSuccessStatusCode,
            cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(
        this HttpClient httpClient,
        Func<HttpRequestMessage, Task> requestBuilder,
        CancellationToken cancellationToken)
        => await SendAsync(
            httpClient,
            requestBuilder,
            completionOption: DefaultHttpCompletionOption,
            ensureSuccessStatusCode: DefaultEnsureSuccessStatusCode,
            cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(
        this HttpClient httpClient,
        Func<HttpRequestMessage, Task> requestBuilder,
        HttpCompletionOption completionOption = DefaultHttpCompletionOption,
        bool ensureSuccessStatusCode = DefaultEnsureSuccessStatusCode,
        CancellationToken cancellationToken = default)
    {
        var request = new HttpRequestMessage();
        await requestBuilder.Invoke(request).ConfigureAwait(false);

        var response = await httpClient.SendAsync(request, completionOption, cancellationToken).ConfigureAwait(false);
        if (ensureSuccessStatusCode)
        {
            try
            {
                response.EnsureSuccessStatusCode();
            }
            catch
            {
                response.Dispose();
                throw;
            }
        }

        return response;
    }

    private static Func<HttpRequestMessage, Task> RequestBuilderToAsync(Action<HttpRequestMessage> requestBuilder)
        => request =>
        {
            requestBuilder.Invoke(request);
            return Task.CompletedTask;
        };
}
