using System;
using jaytwo.Http.Authentication;

namespace jaytwo.Http.Handlers.Authentication;

public class RequestAuthenticationOption : IRequestOption
{
    public static readonly string Key = typeof(RequestAuthenticationOption).FullName!;

    public RequestAuthenticationOption(IAuthenticationProvider authenticationProvider)
    {
        AuthenticationProvider = authenticationProvider;
    }

    public IAuthenticationProvider AuthenticationProvider { get; }

    string IRequestOption.Key => Key;
}
