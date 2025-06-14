public class AuthConfig
{
    public string Issuer { get; set; } = default!;
    public string ClientId { get; set; } = default!;
    public string ClientSecret { get; set; } = default!;
    public string ClientUrl { get; set; } = default!;
    public List<string> RedirectUris { get; set; } = new();
    public List<string> AllowedPostLogoutRedirectUris { get; set; } = new();
    public int AccessTokenLifetimeMinutes { get; set; } = 5;
    public int RefreshTokenLifetimeDays { get; set; } = 30;
}
