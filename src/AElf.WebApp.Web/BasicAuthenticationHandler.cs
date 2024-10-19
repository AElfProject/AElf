using System;
using System.Net.Http.Headers;
using System.Security.Claims;
using System.Text;
using System.Text.Encodings.Web;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AElf.WebApp.Application.Chain;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace AElf.WebApp.Web;

public partial class BasicAuthenticationHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly BasicAuthOptions _basicAuthOptions;

    public BasicAuthenticationHandler(IOptionsMonitor<AuthenticationSchemeOptions> options, ILoggerFactory logger,
        UrlEncoder encoder, ISystemClock clock, IOptionsMonitor<BasicAuthOptions> basicAuthOptions) : base(options,
        logger, encoder, clock)
    {
        _basicAuthOptions = basicAuthOptions.CurrentValue;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        var endpoint = Context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<IAuthorizeData>() == null)
            return Task.FromResult(AuthenticateResult.NoResult());

        if (string.IsNullOrWhiteSpace(_basicAuthOptions.UserName) ||
            string.IsNullOrWhiteSpace(_basicAuthOptions.Password))
        {
            Response.HttpContext.Features.Get<IHttpResponseFeature>().ReasonPhrase = Error.NeedBasicAuth;
            return Task.FromResult(AuthenticateResult.Fail(Error.NeedBasicAuth));
        }

        if (!Request.Headers.ContainsKey("Authorization"))
            return Task.FromResult(AuthenticateResult.Fail("Missing Authorization Header"));

        var (userName, password) = GetUserNameAndPassword();

        if (userName != _basicAuthOptions.UserName || password != _basicAuthOptions.Password)
            return Task.FromResult(AuthenticateResult.Fail("Invalid Username or Password"));

        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, userName),
            new Claim(ClaimTypes.Name, userName)
        };
        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }

    [ExceptionHandler(typeof(Exception), TargetType = typeof(BasicAuthenticationHandler),
        MethodName = nameof(HandleExceptionWhileGettingUserNameAndPassword))]
    private (string, string) GetUserNameAndPassword()
    {
        var authHeader = AuthenticationHeaderValue.Parse(Request.Headers["Authorization"]);
        var credentialBytes = Convert.FromBase64String(authHeader.Parameter);
        var credentials = Encoding.UTF8.GetString(credentialBytes).Split(new[] { ':' }, 2);
        var userName = credentials[0];
        var password = credentials[1];
        return (userName, password);
    }
}