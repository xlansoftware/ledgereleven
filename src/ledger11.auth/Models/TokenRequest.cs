using System.ComponentModel.DataAnnotations;

namespace ledger11.auth.Models;

/// <summary>
/// Represents a token request payload for OAuth 2.0 token endpoint.
/// Supports both authorization_code and refresh_token grant types.
/// </summary>
public class TokenRequest
{
    /// <summary>
    /// Grant type (e.g., "authorization_code" or "refresh_token").
    /// Required for all requests.
    /// </summary>
    [Required]
    public string grant_type { get; set; } = string.Empty;

    /// <summary>
    /// Authorization code (required for grant_type="authorization_code").
    /// </summary>
    public string? code { get; set; }

    /// <summary>
    /// Redirect URI (required if the authorization request included it).
    /// </summary>
    public string? redirect_uri { get; set; }

    /// <summary>
    /// Refresh token (required for grant_type="refresh_token").
    /// </summary>
    public string? refresh_token { get; set; }

    /// <summary>
    /// Client ID (required for public clients without a secret).
    /// </summary>
    public string? client_id { get; set; }

    /// <summary>
    /// Client secret (required for confidential clients).
    /// </summary>
    public string? client_secret { get; set; }

    /// <summary>
    /// Optional scope (must be equal to or narrower than original request).
    /// </summary>
    public string? scope { get; set; }
}