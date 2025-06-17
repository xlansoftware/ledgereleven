using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

[Route(".well-known")]
[ApiController]
public class AuthMetadataController : Controller
{
    private readonly AuthConfig _config;

    public AuthMetadataController(IOptions<AuthConfig> configOptions)
    {
        _config = configOptions.Value;
    }

    [HttpGet("openid-configuration")]
    public IActionResult OidcMetadata()
    {
        if (!Uri.TryCreate(_config.Issuer, UriKind.Absolute, out var issuer))
            return BadRequest("Invalid issuer URL in configuration.");

        return Json(new
        {
            issuer = issuer.ToString(),
            authorization_endpoint = new Uri(issuer, "authorize").ToString(),
            token_endpoint = new Uri(issuer, "token").ToString(),
            userinfo_endpoint = new Uri(issuer, "userinfo").ToString(),
            end_session_endpoint = new Uri(issuer, "logout").ToString(),
            post_logout_redirect_uris = _config.AllowedPostLogoutRedirectUris,
            frontchannel_logout_supported = true,
            backchannel_logout_supported = true,
            frontchannel_logout_session_supported = true,
            jwks_uri = new Uri(issuer, ".well-known/jwks.json").ToString(),
            id_token_signing_alg_values_supported = new[] { "RS256" }
        });
    }

    [HttpGet("jwks.json")]
    public IActionResult Jwks([FromServices] SecurityKey key)
    {
        if (key is not RsaSecurityKey rsaKey || rsaKey.Rsa == null)
            return NotFound();

        var parameters = rsaKey.Rsa.ExportParameters(false);
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
                    n = Base64UrlEncoder.Encode(parameters.Modulus),
                    e = Base64UrlEncoder.Encode(parameters.Exponent)
                }
            }
        };

        return Json(jwk);
    }
}
