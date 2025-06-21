using System.Security.Claims;
using System.Text.Encodings.Web;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;

public class SingleUserAuthHandler : AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly TimeProvider _timeProvider;

    private readonly string _name = "tessa.sterling@xlansoftware.com";
    private readonly string _email = "tessa.sterling@xlansoftware.com";
    private readonly Guid _id = Guid.Parse("a7d3f8b2-4c1e-4f5a-9e6d-3b8c2a1f4e5d");

    public SingleUserAuthHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory logger,
        UrlEncoder encoder,
        TimeProvider timeProvider)
        : base(options, logger, encoder)
    {
        _timeProvider = timeProvider;
    }

    protected override Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // A single user
        var claims = new[]
        {
            new Claim("name", _name),
            new Claim("email", _email),
            new Claim("sub", _id.ToString("N")),
            new Claim(ClaimTypes.NameIdentifier, _id.ToString("N")),
            new Claim(ClaimTypes.Name, _name),
            new Claim(ClaimTypes.Email, _email),
            new Claim("subscription_level", "premium"),
        };

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation($"Single user authenticated at {_timeProvider.GetUtcNow()}");

        return Task.FromResult(AuthenticateResult.Success(ticket));
    }
}