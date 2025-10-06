using System;

namespace jaytwo.Http.Exceptions;

public class RequestTimedOutException : Exception
{
    public RequestTimedOutException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
