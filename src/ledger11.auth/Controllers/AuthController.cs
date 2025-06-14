using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using ledger11.auth.Models;
using Microsoft.AspNetCore.Identity;
using ledger11.auth.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.Extensions.Options;
using System.Text.Json;
using System.Collections.Concurrent;

[Route("")]
public class AuthController : Controller
{
    private static ConcurrentDictionary<string, AuthRequestInfo> _authRequests = new();

    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthController> _logger;
    private readonly AuthConfig _config;
    private readonly ApplicationDbContext _db;
    private readonly ITokenService _tokenService;

    public AuthController(
        IOptions<AuthConfig> configOptions,
        ILogger<AuthController> logger,
        ApplicationDbContext db,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ITokenService tokenService)
    {
        _config = configOptions.Value;
        _logger = logger;
        _db = db;
        _signInManager = signInManager;
        _userManager = userManager;
        _tokenService = tokenService;
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? id_token_hint = null, string? post_logout_redirect_uri = null, string? state = null)
    {
        await _signInManager.SignOutAsync();
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

        // Optional: validate the id_token_hint if needed

        // Validate post_logout_redirect_uri
        if (!string.IsNullOrEmpty(post_logout_redirect_uri))
        {
            var allowedUris = _config.AllowedPostLogoutRedirectUris;

            if (allowedUris.Contains(post_logout_redirect_uri))
            {
                // Append optional state param
                var redirect = string.IsNullOrEmpty(state)
                    ? post_logout_redirect_uri
                    : $"{post_logout_redirect_uri}?state={Uri.EscapeDataString(state)}";

                return Redirect(redirect);
            }
        }

        var clientUrl = _config.ClientUrl;
        if (!string.IsNullOrEmpty(clientUrl))
        {
            return Redirect(clientUrl);
        }
        return Redirect("/");
    }

    // Step 2: OIDC Discovery (optional for testing)
    [HttpGet(".well-known/openid-configuration")]
    public IActionResult OidcMetadata()
    {
        var issuer = _config.Issuer;
        var allowedUris = GetAllowedPostLogoutRedirectUris();
        return Json(new
        {
            issuer,
            authorization_endpoint = $"{issuer}/authorize",
            token_endpoint = $"{issuer}/token",
            userinfo_endpoint = $"{issuer}/userinfo",
            end_session_endpoint = $"{issuer}/logout",
            post_logout_redirect_uris = allowedUris,
            frontchannel_logout_supported = true,
            backchannel_logout_supported = true,
            frontchannel_logout_session_supported = true,
            jwks_uri = $"{issuer}/.well-known/jwks.json",
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }

    [HttpGet(".well-known/jwks.json")]
    public IActionResult Jwks([FromServices] SecurityKey key)
    {
        if (key is not RsaSecurityKey rsaKey)
            return NotFound();

        var parameters = rsaKey.Rsa?.ExportParameters(false);

        var jwk = new
        {
            keys = new[]
            {
            new
            {
                kty = "RSA",
                use = "sig",
                kid = rsaKey.KeyId,
                alg = "RS256",
                n = Base64UrlEncoder.Encode(parameters!.Value.Modulus),
                e = Base64UrlEncoder.Encode(parameters!.Value.Exponent)
            }
        }
        };

        return Json(jwk);
    }

    // Step 3: Handle /authorize
    [HttpGet("authorize")]
    public async Task<IActionResult> Authorize(string response_type, string client_id, string redirect_uri, string scope, string state, string nonce)
    {
        if (_config.ClientId != client_id)
            return BadRequest("Unknown client");

        if (!IsValidRedirectUri(redirect_uri))
            return BadRequest("Invalid redirect URI");

        if (User.Identity != null && User.Identity.IsAuthenticated && User.Identity.Name != null)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name);
            if (user != null)
            {
                // Generate a dummy code and store context
                var code = Guid.NewGuid().ToString("N");
                _authRequests[code] = new AuthRequestInfo
                {
                    UserId = user.Id.ToString("N"),
                    ClientId = client_id,
                    RedirectUri = redirect_uri,
                    Username = user.UserName ?? user.Email ?? user.Id.ToString("N"),
                    Email = user.Email ?? string.Empty,
                    State = state,
                    Nonce = nonce
                };

                return Redirect($"{redirect_uri}?code={code}&state={state}");

            }
        }

        // not logged in or missing user...
        var returnUrl = Url.Action("Authorize", new
        {
            response_type,
            client_id,
            redirect_uri,
            scope,
            state,
            nonce
        });

        return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl!)}");
        // return Redirect($"/login?returnUrl={Uri.EscapeDataString(returnUrl!)}");
    }

    // Step 4: Handle /token
    [HttpPost("token")]
    public async Task<IActionResult> Token([FromForm] TokenRequest request)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { error = "invalid_request" });

        // Validate client credentials
        if (_config.ClientId != request.client_id)
        {
            return BadRequest(new
            {
                error = "invalid_client",
                error_description = "Unknown client"
            });
        }

        if (_config.ClientSecret != request.client_secret)
        {
            return Unauthorized(new
            {
                error = "invalid_client",
                error_description = "Invalid client credentials"
            });
        }

        switch (request.grant_type)
        {
            case "authorization_code":
                return await HandleAuthCodeAsync(request);
            case "refresh_token":
                return await HandleRefreshTokenAsync(request);
            default:
                return BadRequest(new { error = "unsupported_grant_type" });
        }
    }

    private async Task<IActionResult> HandleRefreshTokenAsync(TokenRequest request)
    {
        var refreshToken = await _db.RefreshTokens
            .FirstOrDefaultAsync(t => t.Token == request.refresh_token && !t.Revoked);

        if (refreshToken == null || refreshToken.Expiry < DateTime.UtcNow)
        {
            return BadRequest(new
            {
                error = "invalid_grant",
                error_description = "Invalid or expired refresh token"
            });
        }

        var user = await _userManager.FindByNameAsync(refreshToken.Username);
        if (user == null)
        {
            return BadRequest(new
            {
                error = "invalid_grant",
                error_description = "Invalid or expired refresh token"
            });
        }

        // Revoke old token
        refreshToken.Revoked = true;
        refreshToken.RevokedAt = DateTime.UtcNow;

        var now = DateTimeOffset.UtcNow;
        var issuer = _config.Issuer;
        var username = refreshToken.Username;
        var clientId = refreshToken.ClientId;

        // Create new refresh token (rotation)
        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        var newRefreshToken = await CreateRefreshTokenAsync(username, clientId, ipAddress);

        // Link old token to new one (optional but useful)
        refreshToken.ReplacedByToken = newRefreshToken.Token;

        await _db.SaveChangesAsync();

        var requestInfo = new AuthRequestInfo()
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

    private async Task<IActionResult> HandleAuthCodeAsync(TokenRequest request)
    {
        if (request.grant_type != "authorization_code" || request.code == null || !_authRequests.TryRemove(request.code, out var requestInfo))
        {
            return BadRequest(new
            {
                error = "invalid_grant",
                error_description = "Invalid or expired authorization code"
            });
        }

        if (requestInfo.ClientId != request.client_id || requestInfo.RedirectUri != request.redirect_uri)
        {
            return BadRequest(new
            {
                error = "invalid_request",
                error_description = "Invalid client or redirect URI"
            });
        }

        var accessToken = _tokenService.CreateAccessToken(requestInfo);
        var idToken = _tokenService.CreateIdToken(requestInfo, requestInfo.Nonce);

        var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString();
        // var deviceId = request.device_id; // Optional: add to TokenRequest model if applicable

        var refreshToken = await CreateRefreshTokenAsync(requestInfo.Username, request.client_id, ipAddress, null);

        return Json(new
        {
            access_token = accessToken,
            token_type = "Bearer",
            expires_in = _config.AccessTokenLifetimeMinutes, // 5 minutes
            id_token = idToken,
            refresh_token = refreshToken.Token
        });
    }

    private async Task<RefreshToken> CreateRefreshTokenAsync(string username, string clientId, string? ipAddress = null, string? deviceId = null)
    {
        var refreshToken = new RefreshToken
        {
            Token = Guid.NewGuid().ToString("N"),
            Username = username,
            ClientId = clientId,
            CreatedAt = DateTime.UtcNow,
            Expiry = DateTime.UtcNow.AddDays(_config.RefreshTokenLifetimeDays),
            Revoked = false,
            IpAddress = ipAddress,
            DeviceId = deviceId
        };

        _db.RefreshTokens.Add(refreshToken);
        await _db.SaveChangesAsync();

        return refreshToken;
    }


    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [HttpGet("userinfo")]
    public IActionResult UserInfo()
    {
        if (!User.Identity?.IsAuthenticated ?? false)
            return Unauthorized();

        return Json(new
        {
            sub = User.Identity?.Name ?? "unknown",
            name = User.Identity?.Name ?? "unknown"
        });
    }

    private bool IsValidRedirectUri(string redirectUri)
    {
        if (string.IsNullOrEmpty(redirectUri))
            return false;

        var clientUrl = _config.ClientUrl;
        if (string.IsNullOrEmpty(clientUrl))
            return false;

        if (!redirectUri.StartsWith(clientUrl, StringComparison.OrdinalIgnoreCase))
            return false;

        var uri = new Uri(redirectUri);

        var allowedRedirectUris = _config.RedirectUris;
        return allowedRedirectUris.Contains(uri.AbsolutePath, StringComparer.OrdinalIgnoreCase);
    }

    private List<string>? GetAllowedPostLogoutRedirectUris()
    {
        var clientUrl = _config.ClientUrl;
        if (string.IsNullOrEmpty(clientUrl))
            return null;
        return _config.AllowedPostLogoutRedirectUris
            .Select(x => $"{clientUrl}{x}").ToList();
    }

#if DEBUG

    [HttpPost("testlogin")]
    public IActionResult TestLogin([FromForm] string username = "test")
    {
        var key = HttpContext.RequestServices.GetRequiredService<SecurityKey>();

        var issuer = _config.Issuer;
        if (string.IsNullOrEmpty(issuer))
            return BadRequest("Issuer not configured");

        var clientId = _config.ClientId ?? "client_id";
        var claims = new[]
        {
            new Claim("sub", username),
            new Claim("name", username),
            new Claim(ClaimTypes.Name, username),
            new Claim(JwtRegisteredClaimNames.Iss, issuer),
            new Claim(JwtRegisteredClaimNames.Aud, clientId),
            new Claim(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new Claim(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString()),
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            Issuer = issuer,
            Audience = clientId
        };

        var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
        token.Header["kid"] = ((RsaSecurityKey)key).KeyId;

        var tokenString = tokenHandler.WriteToken(token);

        return Json(new
        {
            access_token = tokenString,
            token_type = "Bearer",
            expires_in = 1800
        });
    }

#endif
}
