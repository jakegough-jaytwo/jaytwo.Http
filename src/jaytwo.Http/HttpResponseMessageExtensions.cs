using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;
using jaytwo.Http.Exceptions;
using jaytwo.Http.Internal;

namespace jaytwo.Http;

public static class HttpResponseMessageExtensions
{
    public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeAsync(this Task<HttpResponseMessage> responseTask)
        => (await responseTask.ConfigureAwait(false)).EnsureSuccessStatusCode();

    public static async Task<HttpResponseMessage> EnsureExpectedStatusCodeAsync(this Task<HttpResponseMessage> responseTask, HttpStatusCode statusCode)
        => (await responseTask.ConfigureAwait(false)).EnsureExpectedStatusCode(statusCode);

    public static HttpResponseMessage EnsureExpectedStatusCode(this HttpResponseMessage response, HttpStatusCode statusCode)
        => response.EnsureExpectedStatusCode(new[] { statusCode });

    public static async Task<HttpResponseMessage> EnsureExpectedStatusCodeAsync(this Task<HttpResponseMessage> responseTask, params HttpStatusCode[] statusCodes)
        => (await responseTask.ConfigureAwait(false)).EnsureExpectedStatusCode(statusCodes);

    public static HttpResponseMessage EnsureExpectedStatusCode(this HttpResponseMessage response, params HttpStatusCode[] statusCodes)
    {
        if (!statusCodes.Contains(response.StatusCode))
        {
            throw new UnexpectedStatusCodeException(response.StatusCode);
        }

        return response;
    }

    public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeOrAsync(this Task<HttpResponseMessage> responseTask, HttpStatusCode statusCode)
        => (await responseTask.ConfigureAwait(false)).EnsureSuccessStatusCodeOr(statusCode);

    public static HttpResponseMessage EnsureSuccessStatusCodeOr(this HttpResponseMessage response, HttpStatusCode statusCode)
        => response.EnsureSuccessStatusCodeOr(new[] { statusCode });

    public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeOrAsync(this Task<HttpResponseMessage> responseTask, params HttpStatusCode[] statusCodes)
        => (await responseTask.ConfigureAwait(false)).EnsureSuccessStatusCodeOr(statusCodes);

    public static HttpResponseMessage EnsureSuccessStatusCodeOr(this HttpResponseMessage response, params HttpStatusCode[] statusCodes)
        => response.EnsureSuccessStatusCodeOr(x => statusCodes.Contains(x.StatusCode));

    public static async Task<HttpResponseMessage> EnsureSuccessStatusCodeOrAsync(this Task<HttpResponseMessage> responseTask, Func<HttpResponseMessage, bool> isAdditionallyAllowed)
        => (await responseTask.ConfigureAwait(false)).EnsureSuccessStatusCodeOr(isAdditionallyAllowed);

    public static HttpResponseMessage EnsureSuccessStatusCodeOr(this HttpResponseMessage response, Func<HttpResponseMessage, bool> isAdditionallyAllowed)
    {
        if (response is null)
        {
            throw new ArgumentNullException(nameof(response));
        }

        if (isAdditionallyAllowed is null)
        {
            throw new ArgumentNullException(nameof(isAdditionallyAllowed));
        }

        if (response.IsSuccessStatusCode || isAdditionallyAllowed(response))
        {
            return response;
        }

        throw new UnexpectedStatusCodeException(response.StatusCode);
    }

    public static async Task<T> AsAnonymousTypeAsync<T>(this Task<HttpResponseMessage> httpResponseTask, T anonymousPrototype)
        => await (await httpResponseTask.ConfigureAwait(false)).AsAnonymousTypeAsync<T>(anonymousPrototype);

    public static async Task<T> AsAnonymousTypeAsync<T>(this HttpResponseMessage httpResponse, T anonymousPrototype)
        => await httpResponse.AsAsync<T>();

    public static async Task<byte[]> AsByteArrayAsync(this Task<HttpResponseMessage> httpResponseTask)
        => await (await httpResponseTask.ConfigureAwait(false)).AsByteArrayAsync();

    public static async Task<byte[]> AsByteArrayAsync(this HttpResponseMessage httpResponse)
    {
        using (httpResponse)
        {
            return await httpResponse.Content.ReadAsByteArrayAsync();
        }
    }

    public static async Task<Stream> AsStreamAsync(this Task<HttpResponseMessage> httpResponseTask)
        => await (await httpResponseTask.ConfigureAwait(false)).AsStreamAsync();

    public static async Task<Stream> AsStreamAsync(this HttpResponseMessage httpResponse)
    {
        // not disposing httpResponse because that only disposes the stream anyway
        return await httpResponse.Content.ReadAsStreamAsync();
    }

    public static async Task<string> AsStringAsync(this Task<HttpResponseMessage> httpResponseTask)
        => await (await httpResponseTask.ConfigureAwait(false)).AsStringAsync();

    public static async Task<string> AsStringAsync(this HttpResponseMessage httpResponse)
    {
        using (httpResponse)
        {
            return await httpResponse.Content.ReadAsStringAsync();
        }
    }

    public static async Task<T> AsAsync<T>(this HttpResponseMessage httpResponse)
    {
        var isJson = false;
        var contentType = httpResponse?.Content?.Headers?.ContentType;

        var asString = default(string);
        if (ContentTypeEvaluator.IsJsonMediaType(contentType))
        {
            isJson = true;
            asString = await httpResponse.AsStringAsync();
        }
        else if (!ContentTypeEvaluator.IsBinaryMediaType(contentType))
        {
            asString = await httpResponse.AsStringAsync();

            if (ContentTypeEvaluator.CouldBeJsonString(asString))
            {
                isJson = true;
            }
        }

        if (isJson)
        {
            return JsonSerializer.Deserialize<T>(asString);
        }

        throw new InvalidOperationException("Data must be JSON to automatically deserialize.");
    }

    public static async Task<T> AsAsync<T>(this Task<HttpResponseMessage> httpResponseTask)
        => await (await httpResponseTask.ConfigureAwait(false)).AsAsync<T>();

    public static async Task<T> ParseWithAsync<T>(this HttpResponseMessage httpResponse, Func<string, T> parseDelegate)
    {
        var asString = await httpResponse.AsStringAsync();
        return parseDelegate.Invoke(asString);
    }

    public static async Task<T> ParseWithAsync<T>(this Task<HttpResponseMessage> httpResponseTask, Func<string, T> parseDelegate)
        => await (await httpResponseTask.ConfigureAwait(false)).ParseWithAsync<T>(parseDelegate);

    public static string GetHeaderValue(this HttpResponseMessage httpResponseMessage, string key)
    {
        return httpResponseMessage.Headers.GetHeaderValue(key) ?? httpResponseMessage.Content?.Headers.GetHeaderValue(key);
    }

    public static string GetHeaderValue(this HttpResponseMessage httpResponseMessage, string key, StringComparison stringComparison)
    {
        return httpResponseMessage.Headers.GetHeaderValue(key, stringComparison) ?? httpResponseMessage.Content?.Headers.GetHeaderValue(key, stringComparison);
    }
}
