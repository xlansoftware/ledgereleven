using System.ComponentModel.DataAnnotations.Schema;
using System.Text.Json.Serialization;

namespace ledger11.model.Data;

/// <summary>
/// Represents a financial transaction within a ledger.
/// </summary>
public class Transaction
{
    /// <summary>
    /// Gets or sets the unique identifier for the transaction.
    /// </summary>
    public int Id { get; set; }

    /// <summary>
    /// Gets or sets the monetary value of the transaction in its original currency.
    /// </summary>
    public decimal Value { get; set; }

    /// <summary>
    /// Gets or sets the ISO 4217 currency code (e.g., "USD", "EUR") of the transaction's value.
    /// </summary>
    public string? Currency { get; set; }

    /// <summary>
    /// Gets or sets the exchange rate used to convert the transaction's value
    /// to the ledger's reporting currency at the time of the transaction.
    /// This value is null if the transaction currency is the same as the reporting currency.
    /// </summary>
    public decimal? ExchangeRate { get; set; }

    /// <summary>
    /// Gets the value of the transaction converted to the ledger's reporting currency.
    /// This is a computed, read-only property not stored in the database.
    /// </summary>
    [NotMapped]
    public decimal ConvertedValue => Value * (ExchangeRate ?? 1.0m);

    /// <summary>
    /// Gets or sets the date and time when the transaction occurred.
    /// </summary>
    public DateTime? Date { get; set; }

    /// <summary>
    /// Gets or sets the foreign key to the associated category.
    /// </summary>
    public int? CategoryId { get; set; }

    /// <summary>
    /// Gets or sets any additional notes or descriptions for the transaction.
    /// </summary>
    public string? Notes { get; set; }

    /// <summary>
    /// Gets or sets the navigation property to the associated Category.
    /// This property is ignored during JSON serialization.
    /// </summary>
    [JsonIgnore]
    public Category? Category { get; set; }

    /// <summary>
    /// Gets or sets the user associated with this transaction.
    /// </summary>
    public string? User { get; set; }

    /// <summary>
    /// Gets or sets the collection of detailed entries for this transaction.
    /// </summary>
    public ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
