using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace jaytwo.Http.Authentication;

public class BasicAuthenticationProvider : AuthenticationProviderBase, IAuthenticationProvider
{
    public BasicAuthenticationProvider(string username, string password)
    {
        User = username;
        Password = password;
    }

    protected internal string User { get; }

    protected internal string Password { get; }

    public override Task AuthenticateAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var combined = $"{User}:{Password}";
        var bytes = Encoding.UTF8.GetBytes(combined);
        var base64 = Convert.ToBase64String(bytes);

        SetRequestAuthenticationHeader(request, "Basic", base64);
        return Task.CompletedTask;
    }
}
