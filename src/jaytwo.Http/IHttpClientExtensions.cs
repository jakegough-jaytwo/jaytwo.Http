using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http;

public static class IHttpClientExtensions
{
    public static Task<HttpResponseMessage> SendAsync(this IHttpClient httpClient, HttpRequestMessage request, CancellationToken cancellationToken)
        => httpClient.SendAsync(request, completionOption: null, cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(this IHttpClient httpClient, Action<HttpRequestMessage> requestBuilderAction, CancellationToken cancellationToken)
        => await httpClient.SendAsync(requestBuilderAction, completionOption: null, cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(this IHttpClient httpClient, Action<HttpRequestMessage> requestBuilderAction, HttpCompletionOption? completionOption = default, CancellationToken? cancellationToken = default)
        => await SendAsync(
            httpClient,
            x =>
            {
                requestBuilderAction.Invoke(x);
                return Task.CompletedTask;
            },
            completionOption,
            cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(this IHttpClient httpClient, Func<HttpRequestMessage, Task> requestBuilderAction, CancellationToken cancellationToken)
        => await httpClient.SendAsync(requestBuilderAction, completionOption: null, cancellationToken: cancellationToken);

    public static async Task<HttpResponseMessage> SendAsync(this IHttpClient httpClient, Func<HttpRequestMessage, Task> requestBuilderAction, HttpCompletionOption? completionOption = default, CancellationToken? cancellationToken = default)
    {
        using var request = new HttpRequestMessage();
        await requestBuilderAction.Invoke(request);
        return await httpClient.SendAsync(request, completionOption, cancellationToken);
    }
}
