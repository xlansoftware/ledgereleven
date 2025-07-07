using System.Text.Json.Serialization;

namespace ledger11.model.Data;

/// <summary>
/// Represents a "space," which is a collection of transactions.
/// This is also referred to as a "ledger" or "book."
/// Each space is designed to be stored in a separate database.
/// </summary>
public class Space
{
    /// <summary>
    /// Gets or sets the unique identifier for the space (Primary Key).
    /// </summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the name of the space.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the ID of the user who created this space.
    /// </summary>
    public Guid CreatedByUserId { get; set; }

    /// <summary>
    /// Gets or sets the UTC date and time when the space was created.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets a string representing the tint or color scheme for the space's UI.
    /// </summary>
    public string? Tint { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., "USD", "EUR") that this space uses for reporting.
    /// All aggregated values and reports for this space will be presented in this currency.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the collection of members associated with this space.
    /// This is a navigation property and is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<SpaceMember> Members { get; set; } = new List<SpaceMember>();
}
