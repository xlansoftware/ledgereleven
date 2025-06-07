using System.Text.Json.Serialization;

namespace ledger11.model.Data;

public class Space
{
    public Guid Id { get; set; } = Guid.NewGuid(); // Primary Key

    public string Name { get; set; } = string.Empty;

    public Guid CreatedByUserId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string? Tint { get; set; }
    public string? Currency { get; set; }

    // Navigation property for space members
    [JsonIgnore]
    public ICollection<SpaceMember> Members { get; set; } = new List<SpaceMember>();
}
