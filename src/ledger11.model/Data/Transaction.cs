using System.Text.Json.Serialization;

namespace ledger11.model.Data;

public class Transaction
{
    public int Id { get; set; }
    public decimal Value { get; set; }
    public DateTime? Date { get; set; }
    public int? CategoryId { get; set; }
    public string? Notes { get; set; }
    
    [JsonIgnore]
    public Category? Category { get; set; }
    public string? User { get; set; }
    public ICollection<TransactionDetail> TransactionDetails { get; set; } = new List<TransactionDetail>();
}
