using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http.Handlers.Logging;

public class HttpClientLogWriter : IHttpClientLogWriter
{
    private const LogLevel RequestResponseLogLevel = LogLevel.Information;

    private readonly ILogger _logger;

    public HttpClientLogWriter(ILogger logger)
    {
        if (logger == null)
        {
            throw new ArgumentNullException(nameof(logger));
        }

        _logger = logger;
    }

    public IDisposable? BeginLoggerScope(out Guid requestId, out string shortRequestId)
    {
        requestId = Guid.NewGuid();
        shortRequestId = requestId.ToString("N").Substring(0, 7);

        var scopeValues = new Dictionary<string, object>
        {
            { Constants.RequestId, requestId },
            { Constants.ShortRequestId, shortRequestId },
        };

        var activity = Activity.Current;
        if (activity != null)
        {
            scopeValues.Add(Constants.TraceId, activity?.TraceId.ToString() ?? string.Empty);
            scopeValues.Add(Constants.SpanId, activity?.SpanId.ToString() ?? string.Empty);
            scopeValues.Add(Constants.ParentSpanId, activity?.ParentSpanId.ToString() ?? string.Empty);
        }

        return _logger.BeginScope(scopeValues);
    }

    public void LogRequest(string shortRequestId, HttpRequestMessage request)
    {
        if (!_logger.IsEnabled(RequestResponseLogLevel))
        {
            return;
        }

        var logBuilder = new LogMessageBuilder();

        AppendPrefix(logBuilder, shortRequestId, EventTypeConstants.REQ);

        logBuilder.Append(
            $"{{{Constants.RequestMethod}}} {{{Constants.RequestUri}}}",
            request.Method,
            request.RequestUri?.OriginalString ?? string.Empty);

        var requestContentType = request.Content?.Headers.ContentType?.MediaType;
        if (!string.IsNullOrEmpty(requestContentType))
        {
            logBuilder.AppendNew($"{{{Constants.RequestContentType}}}", requestContentType!);
        }

        var requestContentLength = request.Content?.Headers.ContentLength;
        if (requestContentLength.HasValue)
        {
            logBuilder.AppendNew($"{{{Constants.RequestContentLength}:n0}} bytes", requestContentLength.Value);
        }

        logBuilder.WriteTo(_logger, HttpClientLogEvents.Request, RequestResponseLogLevel);
    }

    public void LogResponse(string shortRequestId, TimeSpan elapsed, HttpResponseMessage response)
    {
        var logLevel = GetLogLevelForStatusCode(response.StatusCode);
        if (!_logger.IsEnabled(logLevel))
        {
            return;
        }

        var logBuilder = new LogMessageBuilder();

        AppendPrefix(logBuilder, shortRequestId, EventTypeConstants.RES, elapsed);

        logBuilder.Append(
            $"{{{Constants.ResponseStatusCode}}} {{{Constants.ResponseStatus}}}",
            (int)response.StatusCode,
            response.StatusCode.ToString("G"));

        var contentType = response.Content?.Headers?.ContentType?.MediaType;
        if (!string.IsNullOrEmpty(contentType))
        {
            logBuilder.AppendNew($"{{{Constants.ResponseContentType}}}", contentType!);
        }

        var contentLength = response.Content?.Headers?.ContentLength;
        if (contentLength.HasValue)
        {
            logBuilder.AppendNew($"{{{Constants.ResponseContentLength}:n0}} bytes", contentLength.Value);
        }

        logBuilder.WriteTo(_logger, HttpClientLogEvents.Response, logLevel);
    }

    public void LogException(string shortRequestId, TimeSpan elapsed, Exception exception)
    {
        // TODO: this reads funny because it says almost the same thing as LogError by providing the error but not quite the same..
        /*
         *  [9637358] ERR: (8ms) TaskCanceledException A task was canceled.
         *  System.Threading.Tasks.TaskCanceledException: A task was canceled.
         *     at System.Threading.Tasks.TaskCompletionSourceWithCancellation`1.WaitWithCancellationAsync(CancellationToken cancellationToken)
         *     at System.Net.Http.HttpConnectionPool.SendWithVersionDetectionAndRetryAsync(HttpRequestMessage request, Boolean async, Boolean doRequestAuth, CancellationToken cancellationToken)
         *     at System.Net.Http.RedirectHandler.SendAsync(HttpRequestMessage request, Boolean async, CancellationToken cancellationToken)
         *     at jaytwo.Http.Handlers.LoggingDelegatingHandler.SendAsync(HttpRequestMessage request, CancellationToken cancellationToken) in C:\jake\git\jaytwo.Http\src\jaytwo.Http\Handlers\LoggingDelegatingHandler.cs:line 40
         */

        var logBuilder = new LogMessageBuilder();

        AppendPrefix(logBuilder, shortRequestId, EventTypeConstants.ERR, elapsed);

        logBuilder.Append(
            $"{{{Constants.ExceptionType}}} {{{Constants.ExceptionMessage}}}",
            exception.GetType().Name,
            exception.Message);

        logBuilder.WriteErrorTo(_logger, exception);
    }

    private static LogMessageBuilder AppendPrefix(LogMessageBuilder logBuilder, string shortRequestId, string eventType)
        => logBuilder.Append(
            $"[{{{Constants.ShortRequestId}}}] {{{Constants.EventType}}}: ",
            shortRequestId,
            eventType);

    private static LogMessageBuilder AppendPrefix(LogMessageBuilder logBuilder, string shortRequestId, string eventType, TimeSpan elapsed)
        => AppendPrefix(logBuilder, shortRequestId, eventType).Append(
            $"({{{Constants.ElapsedMilliseconds}:n0}}ms) ",
            elapsed.TotalMilliseconds);

    private static LogLevel GetLogLevelForStatusCode(HttpStatusCode statusCode)
    {
        var statusCodeInt = (int)statusCode;
        if (statusCodeInt >= 500)
        {
            return LogLevel.Error;
        }
        else if (statusCodeInt >= 400)
        {
            return LogLevel.Warning;
        }
        else
        {
            return RequestResponseLogLevel;
        }
    }

    public static class Constants
    {
        public const string RequestId = "RequestId";
        public const string ShortRequestId = "ShortRequestId";
        public const string TraceId = "TraceId";
        public const string SpanId = "SpanId";
        public const string ParentSpanId = "ParentSpanId";
        public const string EventType = "EventType";
        public const string RequestMethod = "RequestMethod";
        public const string RequestUri = "RequestUri";
        public const string ElapsedMilliseconds = "ElapsedMilliseconds";
        public const string ResponseStatusCode = "ResponseStatusCode";
        public const string ResponseStatus = "ResponseStatus";
        public const string ExceptionType = "ExceptionType";
        public const string ExceptionMessage = "ExceptionMessage";
        public const string RequestContentType = "RequestContentType";
        public const string RequestContentLength = "RequestContentLength";
        public const string ResponseContentType = "ResponseContentType";
        public const string ResponseContentLength = "ResponseContentLength";
    }
}
