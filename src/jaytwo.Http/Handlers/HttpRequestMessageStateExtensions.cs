using System;
using System.Net.Http;

namespace jaytwo.Http.Handlers;

internal static class HttpRequestMessageStateExtensions
{
    public static void SetState<T>(this HttpRequestMessage request, string key, T state)
    {
#if NET6_0_OR_GREATER
        request.Options.Set(new HttpRequestOptionsKey<T>(key), state);
#else
        request.Properties[key] = state;
#endif
    }

    public static bool TryGetState<T>(this HttpRequestMessage request, string key, out T? result)
#if NET6_0_OR_GREATER
        => request.Options.TryGetValue(new HttpRequestOptionsKey<T>(key), out result);
#else
    {
        if (request.Properties.TryGetValue(key, out var obj) && obj is T typed)
        {
            result = typed;
            return true;
        }

        result = default;
        return false;
    }
#endif
}
