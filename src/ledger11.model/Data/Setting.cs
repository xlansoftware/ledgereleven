using System.ComponentModel.DataAnnotations;

namespace ledger11.model.Data;

/// <summary>
/// Represents a key-value setting within a ledger's database (LedgerDbContext).
/// This allows for storing various configuration options specific to a single ledger.
/// </summary>
public class Setting
{
    /// <summary>
    /// Gets or sets the unique identifier for the setting (Primary Key).
    /// </summary>
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>
    /// Gets or sets the unique business key for the setting (e.g., "IsClosed", "ClosingDate").
    /// This key must be unique within the ledger.
    /// </summary>
    [Required]
    public string Key { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the value of the setting. The value is stored as a string
    /// and should be converted to the appropriate type by the application logic.
    /// </summary>
    public string? Value { get; set; }
}