using ledger11.auth.Data;
using ledger11.auth.Models;
using ledger11.auth.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Caching.Memory;
using System.Security.Cryptography;
using System.Text;
using System.ComponentModel.DataAnnotations;

[Route("")]
[ApiController]
public class AuthTokenController : Controller
{
    private readonly ITokenService _tokenService;
    private readonly IAuthCodeStore _authCodeStore;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ApplicationDbContext _db;
    private readonly AuthConfig _config;
    private readonly ILogger<AuthTokenController> _logger;
    private readonly IMemoryCache _cache;

    // Constants for cache and security
    private const int MaxTokenLength = 2048;
    private const int MaxCodeLength = 512;
    private const int MaxClientIdLength = 256;
    private const string UserCacheKeyPrefix = "user_";
    private static readonly TimeSpan UserCacheExpiry = TimeSpan.FromMinutes(5);

    public AuthTokenController(
        ITokenService tokenService,
        IAuthCodeStore authCodeStore,
        UserManager<ApplicationUser> userManager,
        ApplicationDbContext db,
        IOptions<AuthConfig> configOptions,
        ILogger<AuthTokenController> logger,
        IMemoryCache cache)
    {
        _tokenService = tokenService;
        _authCodeStore = authCodeStore;
        _userManager = userManager;
        _db = db;
        _config = configOptions.Value;
        _logger = logger;
        _cache = cache;
    }

    [EnableRateLimiting("TokenPolicy")]
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        using var scope = _logger.BeginScope(new Dictionary<string, object>
        {
            ["CorrelationId"] = Guid.NewGuid().ToString("N"),
            ["ClientId"] = request?.client_id ?? "unknown",
            ["GrantType"] = request?.grant_type ?? "unknown",
            ["IpAddress"] = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown"
        });

        _logger.LogInformation("Token request received");

        // Enhanced input validation
        var validationResult = ValidateTokenRequest(request);
        if (validationResult != null)
        {
            _logger.LogWarning("Token request validation failed: {Error}", validationResult.error);
            return BadRequest(validationResult);
        }

        // Secure client validation
        if (!IsValidClient(request!))
        {
            _logger.LogWarning("Invalid client credentials");
            return Unauthorized(CreateOAuthError("invalid_client", "Invalid client credentials"));
        }

        return request!.grant_type switch
        {
            "authorization_code" => await HandleAuthCodeAsync(request),
            "refresh_token" => await HandleRefreshTokenAsync(request),
            _ => BadRequest(CreateOAuthError("unsupported_grant_type")),
        };
    }

    private ErrorResponse? ValidateTokenRequest(TokenRequest? request)
    {
        if (request == null || !ModelState.IsValid)
            return CreateOAuthError("invalid_request", "Invalid request format");

        if (string.IsNullOrWhiteSpace(request.client_id) || request.client_id.Length > MaxClientIdLength)
            return CreateOAuthError("invalid_request", "Invalid client_id");

        if (string.IsNullOrWhiteSpace(request.client_secret))
            return CreateOAuthError("invalid_request", "Missing client_secret");

        if (string.IsNullOrWhiteSpace(request.grant_type))
            return CreateOAuthError("invalid_request", "Missing grant_type");

        // Validate grant type specific fields
        switch (request.grant_type)
        {
            case "authorization_code":
                if (string.IsNullOrWhiteSpace(request.code) || request.code.Length > MaxCodeLength)
                    return CreateOAuthError("invalid_request", "Invalid authorization code");
                if (string.IsNullOrWhiteSpace(request.redirect_uri))
                    return CreateOAuthError("invalid_request", "Missing redirect_uri");
                break;

            case "refresh_token":
                if (string.IsNullOrWhiteSpace(request.refresh_token) || request.refresh_token.Length > MaxTokenLength)
                    return CreateOAuthError("invalid_request", "Invalid refresh_token");
                break;
        }

        return null;
    }

    private bool IsValidClient(TokenRequest request)
    {
        // Constant-time comparison for client ID
        var clientIdValid = request.client_id.Length == _config.ClientId.Length &&
                           CryptographicOperations.FixedTimeEquals(
                               Encoding.UTF8.GetBytes(request.client_id),
                               Encoding.UTF8.GetBytes(_config.ClientId));

        // Constant-time comparison for client secret
        var clientSecretValid = request.client_secret.Length == _config.ClientSecret.Length &&
                               CryptographicOperations.FixedTimeEquals(
                                   Encoding.UTF8.GetBytes(request.client_secret),
                                   Encoding.UTF8.GetBytes(_config.ClientSecret));

        return clientIdValid && clientSecretValid;
    }

    private async Task<IActionResult> HandleAuthCodeAsync(TokenRequest request)
    {
        _logger.LogInformation("Processing authorization code grant");

        if (!_authCodeStore.TryRetrieve(request.code!, out var requestInfo) ||
            requestInfo!.ClientId != request.client_id ||
            requestInfo.RedirectUri != request.redirect_uri)
        {
            _logger.LogWarning("Invalid authorization code or mismatched parameters");
            return BadRequest(CreateOAuthError("invalid_grant", "Invalid or expired authorization code"));
        }

        try
        {
            var accessToken = _tokenService.CreateAccessToken(requestInfo);
            var idToken = _tokenService.CreateIdToken(requestInfo, requestInfo.Nonce);
            var refreshToken = await CreateRefreshTokenAsync(
                requestInfo.Username, 
                request.client_id, 
                HttpContext.Connection.RemoteIpAddress?.ToString());

            _logger.LogInformation("Authorization code grant completed successfully for user {Username}", 
                MaskSensitiveData(requestInfo.Username));

            return Json(CreateTokenResponse(accessToken, idToken, refreshToken.Token, _config.AccessTokenLifetimeMinutes * 60));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing authorization code grant");
            return StatusCode(500, CreateOAuthError("server_error", "Internal server error"));
        }
    }

    private async Task<IActionResult> HandleRefreshTokenAsync(TokenRequest request)
    {
        _logger.LogInformation("Processing refresh token grant");

        try
        {
            // Optimized single query with user data
            var refreshTokenData = await _db.RefreshTokens
                .Where(t => t.Token == request.refresh_token && !t.Revoked && t.Expiry > DateTime.UtcNow)
                .Select(t => new { 
                    Token = t, 
                    Username = t.Username,
                    ClientId = t.ClientId,
                    IpAddress = t.IpAddress
                })
                .FirstOrDefaultAsync();

            if (refreshTokenData == null)
            {
                _logger.LogWarning("Invalid or expired refresh token");
                return BadRequest(CreateOAuthError("invalid_grant", "Invalid or expired refresh token"));
            }

            // Check for suspicious activity (IP address change)
            var currentIp = HttpContext.Connection.RemoteIpAddress?.ToString();
            if (!string.IsNullOrEmpty(refreshTokenData.IpAddress) && 
                !string.IsNullOrEmpty(currentIp) && 
                refreshTokenData.IpAddress != currentIp)
            {
                _logger.LogWarning("IP address mismatch for refresh token. Original: {OriginalIp}, Current: {CurrentIp}",
                    refreshTokenData.IpAddress, currentIp);
                // Continue but log for monitoring - you might want to reject in high-security scenarios
            }

            // Get user from cache or database
            var user = await GetUserAsync(refreshTokenData.Username);
            if (user == null)
            {
                _logger.LogWarning("User not found for refresh token: {Username}", MaskSensitiveData(refreshTokenData.Username));
                return BadRequest(CreateOAuthError("invalid_grant", "User not found"));
            }

            // Detect refresh token reuse
            if (!string.IsNullOrEmpty(refreshTokenData.Token.ReplacedByToken))
            {
                _logger.LogWarning("Refresh token reuse detected for user {Username}", MaskSensitiveData(user.UserName));
                await RevokeTokenFamilyAsync(refreshTokenData.Token.Id);
                return BadRequest(CreateOAuthError("invalid_grant", "Token reuse detected"));
            }

            // Revoke old token and create new one
            refreshTokenData.Token.Revoked = true;
            refreshTokenData.Token.RevokedAt = DateTime.UtcNow;

            var newRefreshToken = await CreateRefreshTokenAsync(user.UserName!, refreshTokenData.ClientId, currentIp);
            refreshTokenData.Token.ReplacedByToken = newRefreshToken.Token;

            await _db.SaveChangesAsync();

            var requestInfo = CreateAuthRequestInfo(user);
            var accessToken = _tokenService.CreateAccessToken(requestInfo);
            var idToken = _tokenService.CreateIdToken(requestInfo, "");

            _logger.LogInformation("Refresh token grant completed successfully for user {Username}", 
                MaskSensitiveData(user.UserName));

            return Json(CreateTokenResponse(accessToken, idToken, newRefreshToken.Token, _config.AccessTokenLifetimeMinutes * 60));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing refresh token grant");
            return StatusCode(500, CreateOAuthError("server_error", "Internal server error"));
        }
    }

    private async Task<ApplicationUser?> GetUserAsync(string username)
    {
        var cacheKey = UserCacheKeyPrefix + username;
        
        if (_cache.TryGetValue(cacheKey, out ApplicationUser? cachedUser))
        {
            return cachedUser;
        }

        var user = await _userManager.FindByNameAsync(username);
        if (user != null)
        {
            _cache.Set(cacheKey, user, UserCacheExpiry);
        }

        return user;
    }

    private async Task RevokeTokenFamilyAsync(int tokenId)
    {
        // Find all tokens in the same family and revoke them
        var tokenFamily = await _db.RefreshTokens
            .Where(t => t.Id == tokenId || t.ReplacedByToken != null)
            .ToListAsync();

        foreach (var token in tokenFamily)
        {
            if (!token.Revoked)
            {
                token.Revoked = true;
                token.RevokedAt = DateTime.UtcNow;
            }
        }

        await _db.SaveChangesAsync();
        _logger.LogWarning("Revoked token family containing {TokenCount} tokens", tokenFamily.Count);
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(string username, string clientId, string? ipAddress)
    {
        var token = new RefreshToken
        {
            Token = GenerateSecureToken(),
            Username = username,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Expiry = DateTime.UtcNow.AddDays(_config.RefreshTokenLifetimeDays),
            IpAddress = ipAddress
        };

        _db.RefreshTokens.Add(token);
        await _db.SaveChangesAsync();

        _logger.LogInformation("Created new refresh token for user {Username}", MaskSensitiveData(username));
        return token;
    }

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(bytes);
        }
        return Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');
    }

    private AuthRequestInfo CreateAuthRequestInfo(ApplicationUser user)
    {
        return new AuthRequestInfo
        {
            UserId = user.Id.ToString("N"),
            Username = user.UserName ?? user.Email ?? user.Id.ToString("N"),
            Email = user.Email ?? string.Empty
        };
    }

    private static object CreateTokenResponse(string accessToken, string idToken, string refreshToken, int expiresIn)
    {
        return new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = expiresIn,
            id_token = idToken,
            refresh_token = refreshToken
        };
    }

    private static ErrorResponse CreateOAuthError(string error, string? description = null)
    {
        return description != null 
            ? new ErrorResponse { error = error, error_description = description }
            : new ErrorResponse { error = error };
    }

    private static string MaskSensitiveData(string? data)
    {
        if (string.IsNullOrEmpty(data) || data.Length <= 4)
            return "***";
        
        return data[..2] + "***" + data[^2..];
    }
}

public class ErrorResponse
{
    public string error { get; set; } = string.Empty;
    public string? error_description { get; set; }
}

// Enhanced TokenRequest model with validation attributes
public class TokenRequest
{
    [Required]
    [StringLength(256)]
    public string client_id { get; set; } = string.Empty;

    [Required]
    public string client_secret { get; set; } = string.Empty;

    [Required]
    [RegularExpression("^(authorization_code|refresh_token)$")]
    public string grant_type { get; set; } = string.Empty;

    [StringLength(512)]
    public string? code { get; set; }

    public string? redirect_uri { get; set; }

    [StringLength(2048)]
    public string? refresh_token { get; set; }
}
