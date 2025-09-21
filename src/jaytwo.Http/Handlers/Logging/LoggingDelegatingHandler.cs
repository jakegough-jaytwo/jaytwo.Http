using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http.Handlers.Logging;

public class LoggingDelegatingHandler : DelegatingHandler
{
    public LoggingDelegatingHandler(ILogger logger)
        : base()
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public LoggingDelegatingHandler(HttpMessageHandler innerHandler, ILogger logger)
        : base(innerHandler)
    {
        Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    public ILogger Logger { get; }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // TODO: some kind of abstracted logging formatter

        using var loggerScope = BeginLoggerScope(out var _, out var shortRequestId);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            new LogMessageBuilder(Logger).LogRequest(shortRequestId, request);
            // TODO: request body

            var response = await base.SendAsync(request, cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            new LogMessageBuilder(Logger).LogResponse(shortRequestId, stopwatch.Elapsed, response);
            // TODO: response body

            return response;
        }
        catch (Exception exception)
        {
            stopwatch.Stop();

            new LogMessageBuilder(Logger).LogException(shortRequestId, stopwatch.Elapsed, exception);

            throw;
        }
    }

    private IDisposable BeginLoggerScope(out Guid requestId, out string shortRequestId)
    {
        requestId = Guid.NewGuid();
        shortRequestId = requestId.ToString("N").Substring(0, 7);

        var scopeValues = new Dictionary<string, object>
        {
            { "RequestId", requestId },
            { "ShortRequestId", shortRequestId },
        };

        var activity = Activity.Current;
        if (activity != null)
        {
            scopeValues.Add("TraceId", activity?.TraceId.ToString() ?? string.Empty);
            scopeValues.Add("SpanId", activity?.SpanId.ToString() ?? string.Empty);
            scopeValues.Add("ParentSpanId", activity?.ParentSpanId.ToString() ?? string.Empty);
        }

        return Logger.BeginScope(scopeValues);
    }

    private class LogMessageBuilder
    {
        private const LogLevel RequestResponseLogLevel = LogLevel.Information;

        private readonly ILogger _logger;
        private readonly List<string> _parts = new List<string>();
        private readonly List<object> _parameters = new List<object>();

        public LogMessageBuilder(ILogger logger)
        {
            _logger = logger;
        }

        public void LogRequest(string shortRequestId, HttpRequestMessage request)
        {
            if (!_logger.IsEnabled(RequestResponseLogLevel))
            {
                return;
            }

            Append(
                "[{ShortRequestId}] {EventType}: {RequestMethod} {RequestUri}",
                shortRequestId,
                EventTypeConstants.REQ,
                request.Method,
                request.RequestUri?.OriginalString ?? string.Empty);

            var requestContentType = request.Content?.Headers.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(requestContentType))
            {
                Append("{RequestContentType}", requestContentType);
            }

            var requestContentLength = request.Content?.Headers.ContentLength;
            if (requestContentLength.HasValue)
            {
                Append("{RequestContentLength:n0} bytes", requestContentLength.Value);
            }

            Write(HttpClientLogEvents.Request, RequestResponseLogLevel);
        }

        public void LogResponse(string shortRequestId, TimeSpan elapsed, HttpResponseMessage response)
        {
            var logLevel = GetLogLevelForStatusCode(response.StatusCode);
            if (!_logger.IsEnabled(logLevel))
            {
                return;
            }

            Append(
                "[{ShortRequestId}] {EventType}: ({ElapsedMilliseconds:n0}ms) {ResponseStatusCode} {ResponseStatus}",
                shortRequestId,
                EventTypeConstants.RES,
                elapsed.TotalMilliseconds,
                (int)response.StatusCode,
                response.StatusCode.ToString("G"));

            var contentType = response.Content?.Headers?.ContentType?.MediaType;
            if (!string.IsNullOrEmpty(contentType))
            {
                Append("{ResponseContentType}", contentType);
            }

            var contentLength = response.Content?.Headers?.ContentLength;
            if (contentLength.HasValue)
            {
                Append("{ResponseContentLength:n0} bytes", contentLength.Value);
            }

            Write(HttpClientLogEvents.Response, logLevel);
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

            Append(
                "[{ShortRequestId}] {EventType}: ({ElapsedMilliseconds:n0}ms) {ExceptionType} {ExceptionMessage}",
                shortRequestId,
                EventTypeConstants.ERR,
                elapsed.TotalMilliseconds,
                exception.GetType().Name,
                exception.Message);

            _logger.LogError(HttpClientLogEvents.Error, exception, ToString(), _parameters.ToArray());
        }

        public LogMessageBuilder Append(string messageFormat, params object[] arguments)
        {
            if (!string.IsNullOrEmpty(messageFormat))
            {
                _parts.Add(messageFormat);
            }

            _parameters.AddRange(arguments ?? Array.Empty<object>());

            return this;
        }

        public void Write(EventId eventId, LogLevel logLevel)
        {
            if (_logger.IsEnabled(logLevel))
            {
                _logger.Log(logLevel, eventId, ToString(), _parameters.ToArray());
            }
        }

        public override string ToString()
        {
            return string.Join("; ", _parts);
        }

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
    }

    private class HttpClientLogEvents
    {
        // Choose stable numbers; keep them unique per category
        public static readonly EventId Request = new(1000, EventTypeConstants.REQ);
        public static readonly EventId Response = new(1001, EventTypeConstants.RES);
        public static readonly EventId Error = new(1002, EventTypeConstants.ERR);
    }

    private class EventTypeConstants
    {
        public const string REQ = "REQ";
        public const string RES = "RES";
        public const string ERR = "ERR";
    }
}
