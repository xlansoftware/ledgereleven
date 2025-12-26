using System.Text.Json.Serialization;

namespace ledger11.model.Data;

/// <summary>
/// Represents a "space," which is a collection of transactions.  
/// This is also referred to as a "ledger" or "book."
/// Each space is stored in its own separate database, providing isolation of financial data.
///
/// <para>
/// In the application, the terms <c>Space</c>, <c>Ledger</c>, and <c>Book</c> are sometimes used interchangeably,  
/// but they have distinct meanings and roles:
/// </para>
/// <list type="bullet">
///   <item>
///     <term><c>Space</c></term>
///     <description>
///         A record in the <c>AppDbContext</c> that represents a user's financial workspace.  
///         It holds metadata such as <c>Name</c>, <c>Currency</c>, and a unique <c>Id</c>, which is used to generate  
///         and identify a separate database for the space.
///     </description>
///   </item>
///   <item>
///     <term><c>Ledger</c></term>
///     <description>
///         The actual database created for a given space. It contains all the transactional data,  
///         including transactions, categories, and other financial structures.
///     </description>
///   </item>
///   <item>
///     <term><c>Book</c></term>
///     <description>
///         A user-facing metaphor for a ledger or space. Just like one can have multiple financial books  
///         for different purposes, each book in the app is tied to a unique ledger database and represented  
///         by a corresponding space in the <c>AppDbContext</c>.
///     </description>
///   </item>
/// </list>
///
/// <para>
/// Summary: <c>Book</c> (user concept) → <c>Space</c> (AppDbContext record) → <c>Ledger</c> (underlying database).
/// </para>
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
    [Obsolete("Use Setting with key Tint instead", true)]
    public string? Tint { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., "USD", "EUR") that this space uses for reporting.
    /// All aggregated values and reports for this space will be presented in this currency.
    /// </summary>
    [Obsolete("Use Setting with key Currency instead", true)]
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the collection of members associated with this space.
    /// This is a navigation property and is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public ICollection<SpaceMember> Members { get; set; } = new List<SpaceMember>();
}
