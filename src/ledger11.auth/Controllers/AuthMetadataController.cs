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
        var issuer = _config.Issuer;
        return Json(new
        {
            issuer,
            authorization_endpoint = $"{issuer}/authorize",
            token_endpoint = $"{issuer}/token",
            userinfo_endpoint = $"{issuer}/userinfo",
            end_session_endpoint = $"{issuer}/logout",
            post_logout_redirect_uris = _config.AllowedPostLogoutRedirectUris,
            frontchannel_logout_supported = true,
            backchannel_logout_supported = true,
            frontchannel_logout_session_supported = true,
            jwks_uri = $"{issuer}/.well-known/jwks.json",
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
