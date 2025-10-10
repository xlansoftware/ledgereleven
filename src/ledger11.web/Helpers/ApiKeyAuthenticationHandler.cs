using System.Security.Claims;
using System.Text.Encodings.Web;
using ledger11.data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

public class ApiKeyAuthenticationHandler : Microsoft.AspNetCore.Authentication.AuthenticationHandler<AuthenticationSchemeOptions>
{
    private readonly ILogger<ApiKeyAuthenticationHandler> _logger;
    private readonly IServiceScopeFactory _scopeFactory;

    public const string SchemeName = "ApiKey";

    public ApiKeyAuthenticationHandler(
        IOptionsMonitor<AuthenticationSchemeOptions> options,
        ILoggerFactory loggerFactory,
        UrlEncoder encoder,
        TimeProvider timeProvider,
        IServiceScopeFactory scopeFactory)
        : base(options, loggerFactory, encoder)
    {
        _logger = loggerFactory.CreateLogger<ApiKeyAuthenticationHandler>();
        _scopeFactory = scopeFactory;
    }

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeaderValues))
            return AuthenticateResult.NoResult();

        var providedApiKey = apiKeyHeaderValues.FirstOrDefault();
        if (providedApiKey is null)
            return AuthenticateResult.Fail("Missing API key");

        var providedHash = ApiKeyGenerator.ComputeHash(providedApiKey);

        using var scope = _scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var apiKeyEntity = await db.ApiKeys.FirstOrDefaultAsync(k => k.KeyHash == providedHash);
        if (apiKeyEntity is null || (apiKeyEntity.ExpiresAt != null && apiKeyEntity.ExpiresAt < DateTime.UtcNow))
            return AuthenticateResult.Fail("Invalid or expired API key");

        var user = await db.Users.FirstOrDefaultAsync(k => k.Id == apiKeyEntity.OwnerId);
        if (user is null || !user.EmailConfirmed)
            return AuthenticateResult.Fail("Expired API key");

        var claims = new[]
        {
            new Claim(ClaimTypes.Email, user.Email ?? user.UserName ?? user.Id.ToString()),
            new Claim(ClaimTypes.NameIdentifier, apiKeyEntity.OwnerId.ToString()),
            new Claim(ClaimTypes.Name, "ApiKeyUser")
        };

        var identity = new ClaimsIdentity(claims, SchemeName);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, SchemeName);

        return AuthenticateResult.Success(ticket);
    }
}
