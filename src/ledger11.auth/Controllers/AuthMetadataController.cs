using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

[Route(".well-known")]
[ApiController]
public class AuthMetadataController : ControllerBase
{
    private readonly AuthConfig _config;
    private readonly ILogger<AuthMetadataController> _logger;

    public AuthMetadataController(IOptions<AuthConfig> configOptions, ILogger<AuthMetadataController> logger)
    {
        _config = configOptions?.Value ?? throw new ArgumentNullException(nameof(configOptions));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Returns OpenID Connect discovery metadata according to RFC 8414
    /// </summary>
    /// <returns>OIDC discovery document</returns>
    [HttpGet("openid-configuration")]
    [ProducesResponseType(typeof(OidcMetadataResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(500)]
    public IActionResult OidcMetadata()
    {
        try
        {
            // Validate configuration
            var validationResult = ValidateAuthConfig(_config);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid configuration detected: {Errors}",
                    string.Join(", ", validationResult.Errors));
                return BadRequest("Invalid server configuration.");
            }

            foreach (var uri in _config.AllowedPostLogoutRedirectUris)
            {
                _logger.LogInformation("AllowedPostLogoutRedirectUris: {Uri}", uri);
            }

            var issuer = new Uri(_config.Issuer);
            var response = new OidcMetadataResponse
            {
                Issuer = issuer.ToString(),
                AuthorizationEndpoint = BuildEndpointUri(issuer, "authorize"),
                TokenEndpoint = BuildEndpointUri(issuer, "token"),
                UserinfoEndpoint = BuildEndpointUri(issuer, "userinfo"),
                EndSessionEndpoint = BuildEndpointUri(issuer, "logout"),
                PostLogoutRedirectUris = SanitizeRedirectUris(_config.AllowedPostLogoutRedirectUris),
                FrontchannelLogoutSupported = true,
                BackchannelLogoutSupported = true,
                FrontchannelLogoutSessionSupported = true,
                JwksUri = BuildEndpointUri(issuer, ".well-known/jwks.json"),
                IdTokenSigningAlgValuesSupported = new[] { "RS256" }
            };

            _logger.LogDebug("Successfully generated OIDC metadata for issuer: {Issuer}", issuer);
            return Ok(response);
        }
        catch (UriFormatException ex)
        {
            _logger.LogError(ex, "Invalid issuer URI format: {Issuer}", _config?.Issuer);
            return BadRequest("Invalid issuer URL format.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating OIDC metadata");
            return StatusCode(500, "Internal server error occurred.");
        }
    }

    /// <summary>
    /// Returns JSON Web Key Set (JWKS) for token signature verification
    /// </summary>
    /// <param name="key">RSA security key for signing</param>
    /// <returns>JWKS document</returns>
    [HttpGet("jwks.json")]
    [ProducesResponseType(typeof(JwksResponse), 200)]
    [ProducesResponseType(400)]
    [ProducesResponseType(404)]
    [ProducesResponseType(500)]
    public IActionResult Jwks([FromServices] SecurityKey key)
    {
        try
        {
            // Comprehensive input validation
            var validationResult = ValidateSecurityKey(key);
            if (!validationResult.IsValid)
            {
                _logger.LogWarning("Invalid security key: {Errors}",
                    string.Join(", ", validationResult.Errors));
                return BadRequest("Invalid security key configuration.");
            }

            var rsaKey = (RsaSecurityKey)key;
            var parameters = rsaKey.Rsa!.ExportParameters(false);

            // Validate RSA parameters
            if (!ValidateRsaParameters(parameters))
            {
                _logger.LogError("Invalid RSA parameters detected");
                return BadRequest("Invalid RSA key parameters.");
            }

            var jwk = new JwksResponse
            {
                Keys = new[]
                {
                    new JsonWebKey
                    {
                        KeyType = "RSA",
                        Use = "sig",
                        KeyId = SanitizeKeyId(rsaKey.KeyId),
                        Algorithm = "RS256",
                        Modulus = Base64UrlEncoder.Encode(parameters.Modulus!),
                        Exponent = Base64UrlEncoder.Encode(parameters.Exponent!)
                    }
                }
            };

            _logger.LogDebug("Successfully generated JWKS with key ID: {KeyId}", rsaKey.KeyId);
            return Ok(jwk);
        }
        catch (CryptographicException ex)
        {
            _logger.LogError(ex, "Cryptographic error exporting RSA parameters");
            return StatusCode(500, "Error processing security key.");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error generating JWKS");
            return StatusCode(500, "Internal server error occurred.");
        }
    }

    #region Private Validation Methods

    private ValidationResult ValidateAuthConfig(AuthConfig config)
    {
        var result = new ValidationResult();

        if (config == null)
        {
            result.AddError("Configuration is null");
            return result;
        }

        // Validate issuer
        if (string.IsNullOrWhiteSpace(config.Issuer))
        {
            result.AddError("Issuer is required");
        }
        else if (!Uri.TryCreate(config.Issuer, UriKind.Absolute, out var issuerUri))
        {
            result.AddError("Issuer must be a valid absolute URI");
        }
        else if (issuerUri.Scheme != "https" && !IsLocalhost(issuerUri))
        {
            result.AddError("Issuer must use HTTPS in production environments");
        }

        // Validate redirect URIs
        if (config.AllowedPostLogoutRedirectUris != null)
        {
            foreach (var uri in config.AllowedPostLogoutRedirectUris)
            {
                if (!string.IsNullOrWhiteSpace(uri) && !Uri.TryCreate(uri, UriKind.Absolute, out _))
                {
                    result.AddError($"Invalid redirect URI: {uri}");
                }
            }
        }

        return result;
    }

    private ValidationResult ValidateSecurityKey(SecurityKey key)
    {
        var result = new ValidationResult();

        if (key == null)
        {
            result.AddError("Security key is null");
            return result;
        }

        if (key is not RsaSecurityKey rsaKey)
        {
            result.AddError("Security key must be of type RsaSecurityKey");
            return result;
        }

        if (rsaKey.Rsa == null)
        {
            result.AddError("RSA instance is null");
            return result;
        }

        if (string.IsNullOrWhiteSpace(rsaKey.KeyId))
        {
            result.AddError("Key ID is required");
        }

        // Validate key size
        try
        {
            var keySize = rsaKey.Rsa.KeySize;
            if (keySize < 2048)
            {
                result.AddError($"RSA key size ({keySize}) is below minimum requirement (2048)");
            }
        }
        catch (Exception ex)
        {
            result.AddError($"Unable to determine key size: {ex.Message}");
        }

        return result;
    }

    private bool ValidateRsaParameters(RSAParameters parameters)
    {
        // Validate required public key components
        if (parameters.Modulus == null || parameters.Modulus.Length == 0)
        {
            _logger.LogError("RSA modulus is null or empty");
            return false;
        }

        if (parameters.Exponent == null || parameters.Exponent.Length == 0)
        {
            _logger.LogError("RSA exponent is null or empty");
            return false;
        }

        // Basic sanity checks
        if (parameters.Modulus.Length < 256) // 2048-bit key = 256 bytes
        {
            _logger.LogError("RSA modulus too short: {Length} bytes", parameters.Modulus.Length);
            return false;
        }

        return true;
    }

    private string[] SanitizeRedirectUris(List<string> uris)
    {
        if (uris == null) return Array.Empty<string>();

        return uris
            .Where(uri => !string.IsNullOrWhiteSpace(uri))
            .Where(uri => Uri.TryCreate(uri, UriKind.Absolute, out var parsedUri) &&
                         (parsedUri.Scheme == "https" || IsLocalhost(parsedUri)))
            .Select(uri => uri.Trim())
            .ToArray();
    }

    private string SanitizeKeyId(string? keyId)
    {
        if (string.IsNullOrWhiteSpace(keyId))
            return Guid.NewGuid().ToString("N");

        // Remove potentially dangerous characters and limit length
        var sanitized = new string(keyId
            .Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_')
            .Take(64)
            .ToArray());

        return string.IsNullOrEmpty(sanitized) ? Guid.NewGuid().ToString("N") : sanitized;
    }

    private string BuildEndpointUri(Uri issuer, string endpoint)
    {
        return new Uri(issuer, endpoint).ToString();
    }

    private bool IsLocalhost(Uri uri)
    {
        return uri.Host.Equals("localhost", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("127.0.0.1", StringComparison.OrdinalIgnoreCase) ||
               uri.Host.Equals("::1", StringComparison.OrdinalIgnoreCase);
    }

    #endregion
}

#region Data Models

public class OidcMetadataResponse
{
    [Required]
    [JsonPropertyName("issuer")]
    public string Issuer { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("authorization_endpoint")]
    public string AuthorizationEndpoint { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("token_endpoint")]
    public string TokenEndpoint { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("userinfo_endpoint")]
    public string UserinfoEndpoint { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("end_session_endpoint")]
    public string EndSessionEndpoint { get; set; } = string.Empty;

    [JsonPropertyName("post_logout_redirect_uris")]
    public string[] PostLogoutRedirectUris { get; set; } = Array.Empty<string>();

    [JsonPropertyName("frontchannel_logout_supported")]
    public bool FrontchannelLogoutSupported { get; set; }

    [JsonPropertyName("backchannel_logout_supported")]
    public bool BackchannelLogoutSupported { get; set; }

    [JsonPropertyName("frontchannel_logout_session_supported")]
    public bool FrontchannelLogoutSessionSupported { get; set; }

    [Required]
    [JsonPropertyName("jwks_uri")]
    public string JwksUri { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("id_token_signing_alg_values_supported")]
    public string[] IdTokenSigningAlgValuesSupported { get; set; } = Array.Empty<string>();
}

public class JwksResponse
{
    [Required]
    public JsonWebKey[] Keys { get; set; } = Array.Empty<JsonWebKey>();
}

public class JsonWebKey
{
    [Required]
    [JsonPropertyName("kty")]
    public string KeyType { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("use")]
    public string Use { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("kid")]
    public string KeyId { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("alg")]
    public string Algorithm { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("n")]
    public string Modulus { get; set; } = string.Empty;

    [Required]
    [JsonPropertyName("e")]
    public string Exponent { get; set; } = string.Empty;
}

public class ValidationResult
{
    private readonly List<string> _errors = new();

    public bool IsValid => _errors.Count == 0;
    public IReadOnlyList<string> Errors => _errors.AsReadOnly();

    public void AddError(string error)
    {
        _errors.Add(error);
    }
}

#endregion
