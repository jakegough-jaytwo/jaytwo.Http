#if !NET6_0_OR_GREATER

using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Security;

namespace jaytwo.Http;

public partial class HttpClientBuilder
{
    private readonly List<Action<HttpClientHandler>> _handlerConfigurations = new List<Action<HttpClientHandler>>();

    public HttpClientBuilder WithRemoteCertificateValidationCallback(RemoteCertificateValidationCallback callback)
    {
        if (callback == null)
        {
            throw new ArgumentNullException(nameof(callback));
        }

        return ConfigureHandler(handler =>
            handler.ServerCertificateCustomValidationCallback =
                (req, cert, chain, errors) => callback(req, cert, chain, errors));
    }

    public HttpClientBuilder ConfigureHandler(Action<HttpClientHandler> configuration)
    {
        if (configuration == null)
        {
            throw new ArgumentNullException(nameof(configuration));
        }

        _handlerConfigurations.Add(configuration);
        return this;
    }

    private static HttpClientHandler CreateDefaultPrimaryHandler()
        => new HttpClientHandler();
}
#endif
