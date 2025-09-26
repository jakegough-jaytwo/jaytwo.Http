using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace jaytwo.Http.Handlers.Logging;

internal class LogMessageBuilder
{
    private readonly List<string> _parts = new List<string>();
    private readonly List<object> _parameters = new List<object>();

    public LogMessageBuilder()
    {
    }

    public LogMessageBuilder Append(string messageFormat, params object[] arguments)
    {
        messageFormat ??= string.Empty;

        var index = _parts.Count - 1;

        if (index < 0)
        {
            _parts.Add(messageFormat);
        }
        else
        {
            _parts[index] += messageFormat;
        }

        _parameters.AddRange(arguments ?? Array.Empty<object>());

        return this;
    }

    public LogMessageBuilder AppendNew(string messageFormat, params object[] arguments)
    {
        if (!string.IsNullOrEmpty(messageFormat))
        {
            _parts.Add(messageFormat);
        }

        _parameters.AddRange(arguments ?? Array.Empty<object>());

        return this;
    }

    public void WriteTo(ILogger logger, EventId eventId, LogLevel logLevel)
    {
        if (logger.IsEnabled(logLevel))
        {
            logger.Log(logLevel, eventId, ToString(), _parameters.ToArray());
        }
    }

    public void WriteErrorTo(ILogger logger, Exception exception)
    {
        if (logger.IsEnabled(LogLevel.Error))
        {
            logger.LogError(HttpClientLogEvents.Error, exception, ToString(), _parameters.ToArray());
        }
    }

    public override string ToString()
    {
        return string.Join("; ", _parts);
    }
}
