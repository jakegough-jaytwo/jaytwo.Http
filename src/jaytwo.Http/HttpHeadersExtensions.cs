using System;
using System.Linq;
using System.Net.Http.Headers;

namespace jaytwo.Http;

public static class HttpHeadersExtensions
{
    public static string? GetHeaderValue(this HttpHeaders httpHeaders, string key)
        => GetHeaderValue(httpHeaders, key, StringComparison.OrdinalIgnoreCase);

    public static string? GetHeaderValue(this HttpHeaders httpHeaders, string key, StringComparison stringComparison)
        => httpHeaders.FirstOrDefault(x => string.Equals(x.Key, key, stringComparison)).Value?.FirstOrDefault();

    internal static void AddSmartly(this HttpHeaders httpHeaders, string name, string? value)
    {
        switch (name)
        {
            default:
                httpHeaders.Add(name, value);
                break;
        }
    }
}
