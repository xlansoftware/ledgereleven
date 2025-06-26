using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using ledger11.auth.Models;
using ledger11.auth.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[Route("")]
[ApiController]
public class AuthSessionController : Controller
{
    private readonly IAuthCodeStore _authCodeStore;
    private readonly SignInManager<ApplicationUser> _signInManager;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ILogger<AuthSessionController> _logger;
    private readonly AuthConfig _config;
    private readonly IHostEnvironment _env;
    private readonly TokenValidationParameters _tokenValidationParameters;
    private readonly JwtSecurityTokenHandler _tokenHandler = new();

    public AuthSessionController(
        IOptions<AuthConfig> configOptions,
        SignInManager<ApplicationUser> signInManager,
        UserManager<ApplicationUser> userManager,
        ILogger<AuthSessionController> logger,
        IAuthCodeStore authCodeStore,
        IHostEnvironment env,
        IOptionsMonitor<JwtBearerOptions> jwtOptions)
    {
        _authCodeStore = authCodeStore;
        _signInManager = signInManager;
        _userManager = userManager;
        _logger = logger;
        _config = configOptions.Value;
        _env = env;
        _tokenValidationParameters = jwtOptions.Get(JwtBearerDefaults.AuthenticationScheme).TokenValidationParameters;
    }

    [HttpGet("authorize")]
    [EnableRateLimiting("TokenPolicy")]
    public async Task<IActionResult> Authorize(string response_type, string client_id, string redirect_uri, string scope, string state, string nonce)
    {
        if (_config.ClientId != client_id || !IsValidRedirectUri(redirect_uri))
            return BadRequest("Invalid client or redirect URI");

        if (User.Identity?.IsAuthenticated == true)
        {
            var user = await _userManager.FindByNameAsync(User.Identity.Name!);
            if (user != null)
            {
                var code = Guid.NewGuid().ToString("N");
                _authCodeStore.Store(code, new AuthRequestInfo
                {
                    UserId = user.Id.ToString("N"),
                    ClientId = client_id,
                    RedirectUri = redirect_uri,
                    Username = user.UserName ?? user.Email ?? user.Id.ToString("N"),
                    Email = user.Email ?? string.Empty,
                    State = state,
                    Nonce = nonce
                });

                return Redirect($"{redirect_uri}?code={code}&state={state}");
            }
        }

        var returnUrl = Url.Action("Authorize", new { response_type, client_id, redirect_uri, scope, state, nonce });
        return Redirect($"/Identity/Account/Login?returnUrl={Uri.EscapeDataString(returnUrl!)}");
    }

    [HttpGet("logout")]
    public async Task<IActionResult> Logout(string? id_token_hint = null, string? post_logout_redirect_uri = null, string? state = null)
    {
        Response.Headers["Cache-Control"] = "no-store";
        Response.Headers["Pragma"] = "no-cache";

        if (!string.IsNullOrEmpty(id_token_hint) && ValidateIdTokenHint(id_token_hint) == null)
        {
            _logger.LogWarning("Invalid id_token_hint provided during logout.");
            return BadRequest("Invalid ID token hint.");
        }

        if (!string.IsNullOrEmpty(post_logout_redirect_uri) &&
            _config.AllowedPostLogoutRedirectUris.Contains(post_logout_redirect_uri))
        {
            var redirect = string.IsNullOrEmpty(state) ? post_logout_redirect_uri :
                $"{post_logout_redirect_uri}?state={Uri.EscapeDataString(state)}";

            await _signInManager.SignOutAsync();
            return Redirect(redirect);
        }

        await _signInManager.SignOutAsync();
        return Redirect(_config.ClientUrl ?? "/");
    }

    [HttpGet("userinfo")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
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

    private ClaimsPrincipal? ValidateIdTokenHint(string idToken)
    {
        try
        {
            return _tokenHandler.ValidateToken(idToken, _tokenValidationParameters, out _);
        }
        catch (SecurityTokenException ex)
        {
            _logger.LogWarning("Invalid id_token_hint: {Error}", ex.Message);
            return null;
        }
    }

    private bool IsValidRedirectUri(string redirectUri)
    {
        if (!Uri.TryCreate(redirectUri, UriKind.Absolute, out var uri))
        {
            _logger.LogWarning("Invalid redirect URI: {Uri}. Should be absolute URL", redirectUri);
            return false;
        }

        if (!_env.IsDevelopment() && !uri.Scheme.Equals("https", StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Redirect URI {Uri} is not HTTPS. Only secure URIs are allowed in production.", redirectUri);
            return false;
        }

        if (!uri.AbsoluteUri.StartsWith(_config.ClientUrl, StringComparison.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Redirect URI {Uri} does not match the configured client URL: {ClientUrl}", redirectUri, _config.ClientUrl);
            return false;
        }

        if (!_config.RedirectUris.Contains(uri.AbsolutePath, StringComparer.OrdinalIgnoreCase))
        {
            _logger.LogWarning("Redirect URI {Uri} is not in the allowed list: {AllowedUris}", redirectUri, string.Join(", ", _config.RedirectUris));
            return false;
        }

        _logger.LogDebug("Redirect URI {Uri} is valid", redirectUri);
        return true;
    }

#if DEBUG
    [HttpPost("testlogin")]
    public IActionResult TestLogin([FromForm] string username = "test")
    {
        var key = HttpContext.RequestServices.GetRequiredService<SecurityKey>();
        var issuer = _config.Issuer;
        var clientId = _config.ClientId;

        var claims = new List<Claim>
        {
            new("sub", username),
            new("name", username),
            new(ClaimTypes.Name, username),
            new(JwtRegisteredClaimNames.Iss, issuer),
            new(JwtRegisteredClaimNames.Aud, clientId),
            new(JwtRegisteredClaimNames.Iat, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString()),
            new(JwtRegisteredClaimNames.Exp, DateTimeOffset.UtcNow.AddMinutes(30).ToUnixTimeSeconds().ToString())
        };

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(30),
            SigningCredentials = new SigningCredentials(key, SecurityAlgorithms.RsaSha256),
            Issuer = issuer,
            Audience = clientId
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        var token = tokenHandler.CreateJwtSecurityToken(tokenDescriptor);
        token.Header["kid"] = ((RsaSecurityKey)key).KeyId;

        return Json(new
        {
            access_token = tokenHandler.WriteToken(token),
            token_type = "Bearer",
            expires_in = 1800
        });
    }
#endif
}
