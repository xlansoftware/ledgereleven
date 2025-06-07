using System.Text.Json.Serialization;

namespace ledger11.model.Data;

public class TransactionDetail
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public string? Description { get; set; } = string.Empty;
    public decimal? Quantity { get; set; }
    
    public int? CategoryId { get; set; }
    [JsonIgnore]
    public Category? Category { get; set; }

    // foreign key to transaction
    public int? TransactionId { get; set; }

    [JsonIgnore]
    public Transaction? Transaction { get; set; }


}
