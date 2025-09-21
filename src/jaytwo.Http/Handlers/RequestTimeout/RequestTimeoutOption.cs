using System;

namespace jaytwo.Http.Handlers.RequestTimeout;

public class RequestTimeoutOption : IRequestOption
{
    public static readonly string Key = typeof(RequestTimeoutOption).FullName!;

    public RequestTimeoutOption(TimeSpan timeout)
    {
        Timeout = timeout;
    }

    public TimeSpan Timeout { get; }

    string IRequestOption.Key => Key;
}
