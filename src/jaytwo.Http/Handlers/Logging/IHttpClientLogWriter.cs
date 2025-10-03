using System;
using System.Net.Http;

namespace jaytwo.Http.Handlers.Logging;

public interface IHttpClientLogWriter
{
    IDisposable? BeginLoggerScope(out Guid requestId, out string shortRequestId);

    void LogRequest(string shortRequestId, HttpRequestMessage request);

    void LogResponse(string shortRequestId, TimeSpan elapsed, HttpResponseMessage response);

    void LogException(string shortRequestId, TimeSpan elapsed, Exception exception);
}
