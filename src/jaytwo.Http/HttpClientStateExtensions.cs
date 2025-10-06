using System;
using System.Collections.Concurrent;
using System.Net.Http;
using System.Runtime.CompilerServices;

namespace jaytwo.Http;

internal static class HttpClientStateExtensions
{
    private static readonly ConditionalWeakTable<HttpClient, ConcurrentDictionary<string, object>> _bag = new();

    public static void SetState<T>(this HttpClient client, string key, T value) =>
        _bag.GetOrCreateValue(client)[key] = value!;

    public static bool TryGetState<T>(this HttpClient client, string key, out T? value)
    {
        value = default;
        return _bag.GetOrCreateValue(client).TryGetValue(key, out var obj)
            && obj is T t && (value = t) is not null;
    }

    public static T GetOrAddState<T>(this HttpClient client, string key, Func<string, T> factory)
    {
        var obj = _bag.GetOrCreateValue(client).GetOrAdd(key, x => factory(x)!);
        if (obj is T result)
        {
            return result;
        }
        else
        {
            throw new Exception("Object is not typed correctly");
        }
    }

    public static HttpClientContext GetContext(this HttpClient client)
        => HttpClientContext.GetContext(client);
}
