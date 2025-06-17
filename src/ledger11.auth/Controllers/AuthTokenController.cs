using ledger11.auth.Data;
using ledger11.auth.Models;
using ledger11.auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

[Route("")]
[ApiController]
public class AuthTokenController : Controller
{
    private readonly ITokenService _tokenService;
    private readonly IAuthCodeStore _authCodeStore;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly AuthConfig _config;

    public AuthTokenController(
        ITokenService tokenService,
        IAuthCodeStore authCodeStore,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<AuthConfig> configOptions)
    {
        _tokenService = tokenService;
        _authCodeStore = authCodeStore;
        _userManager = userManager;
        _db = db;
        _config = configOptions.Value;
    }

    [EnableRateLimiting("TokenPolicy")]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "invalid_request" });

        if (_config.ClientId != request.client_id)
            return BadRequest(new { error = "invalid_client", error_description = "Unknown client" });

        if (_config.ClientSecret != request.client_secret)
            return Unauthorized(new { error = "invalid_client", error_description = "Invalid client credentials" });

        return request.grant_type switch
        {
            "authorization_code" => await HandleAuthCodeAsync(request),
            "refresh_token" => await HandleRefreshTokenAsync(request),
            _ => BadRequest(new { error = "unsupported_grant_type" }),
        };
    }

    private async Task<IActionResult> HandleAuthCodeAsync(TokenRequest request)
    {
        if (!_authCodeStore.TryRetrieve(request.code!, out var requestInfo) ||
            requestInfo!.ClientId != request.client_id ||
            requestInfo.RedirectUri != request.redirect_uri)
        {
            return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired authorization code" });
        }

        var accessToken = _tokenService.CreateAccessToken(requestInfo);
        var idToken = _tokenService.CreateIdToken(requestInfo, requestInfo.Nonce);
        var refreshToken = await CreateRefreshTokenAsync(requestInfo.Username, request.client_id, HttpContext.Connection.RemoteIpAddress?.ToString());

        return Json(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _config.AccessTokenLifetimeMinutes,
            id_token = idToken,
            refresh_token = refreshToken.Token
        });
    }

    private async Task<IActionResult> HandleRefreshTokenAsync(TokenRequest request)
    {
        var refreshToken = await _db.RefreshTokens.FirstOrDefaultAsync(t => t.Token == request.refresh_token && !t.Revoked);
        if (refreshToken == null || refreshToken.Expiry < DateTime.UtcNow)
            return BadRequest(new { error = "invalid_grant", error_description = "Invalid or expired refresh token" });

        var user = await _userManager.FindByNameAsync(refreshToken.Username);
        if (user == null)
            return BadRequest(new { error = "invalid_grant", error_description = "User not found" });

        refreshToken.Revoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var newRefreshToken = await CreateRefreshTokenAsync(user.UserName!, refreshToken.ClientId, HttpContext.Connection.RemoteIpAddress?.ToString());
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _db.SaveChangesAsync();

        var requestInfo = new AuthRequestInfo
        {
            UserId = user.Id.ToString("N"),
            Username = user.UserName ?? user.Email ?? user.Id.ToString("N"),
            Email = user.Email ?? string.Empty
        };

        var accessToken = _tokenService.CreateAccessToken(requestInfo);
        var idToken = _tokenService.CreateIdToken(requestInfo, "");

        return Json(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _config.RefreshTokenLifetimeDays * 60,
            id_token = idToken,
            refresh_token = newRefreshToken.Token
        });
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(string username, string clientId, string? ipAddress)
    {
        var token = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            Username = username,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Expiry = DateTime.UtcNow.AddDays(_config.RefreshTokenLifetimeDays),
            IpAddress = ipAddress
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        return token;
    }
}
