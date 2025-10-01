using System;
using System.Net;
using System.Net.Http;

namespace jaytwo.Http.Exceptions;

public class UnexpectedStatusCodeException : HttpRequestException
{
    // TODO: ActualStatusCode, ExpectedStatusCodes

    public UnexpectedStatusCodeException(HttpStatusCode statusCode)
#if NET6_0_OR_GREATER
        : base(GetMessage(statusCode), null, statusCode)
#else
        : base(GetMessage(statusCode))
#endif
    {
#if !NET6_0_OR_GREATER
        StatusCode = statusCode;
#endif
    }

#if !NET6_0_OR_GREATER
    public HttpStatusCode StatusCode { get; }
#endif

    private static string GetMessage(HttpStatusCode statusCode)
    {
        return $"Unexpected status code: {(int)statusCode} ({statusCode})";
    }
}
