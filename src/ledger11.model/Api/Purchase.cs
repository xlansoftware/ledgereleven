namespace ledger11.model.Api;
public class Purchase
{
    public int? Id { get; set; }
    public decimal? TotalAmount { get; set; }
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public int? Quantity { get; set; }
    public DateTime? Date { get; set; }
    public int? ParentPurchaseId { get; set; }
    public string? Category { get; set; }
}
