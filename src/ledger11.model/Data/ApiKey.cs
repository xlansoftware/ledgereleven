public class ApiKey
{
    public int Id { get; set; }
    public string KeyHash { get; set; } = default!;
    public Guid OwnerId { get; set; } = default!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? ExpiresAt { get; set; }
}