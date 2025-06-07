public class ExternalLogin
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string? Provider { get; set; }         // oidc issuer
    public string? ProviderUserId { get; set; }   // The "sub" claim
}