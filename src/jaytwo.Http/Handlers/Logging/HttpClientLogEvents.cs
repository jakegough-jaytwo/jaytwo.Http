using System;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http.Handlers.Logging;

internal static class HttpClientLogEvents
{
    // Choose stable numbers; keep them unique per category
    public static readonly EventId Request = new(1000, EventTypeConstants.REQ);
    public static readonly EventId Response = new(1001, EventTypeConstants.RES);
    public static readonly EventId Error = new(1002, EventTypeConstants.ERR);
}
