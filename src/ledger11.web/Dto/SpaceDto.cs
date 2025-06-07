namespace ledger11.model.Api;

public class SpaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Tint { get; set; } = "#FFFFFF";
    public string? Currency { get; set; } = "USD";

    public decimal? TotalValue { get; set; } = null;
    public int? CountTransactions { get; set; } = null;
    public int? CountCategories { get; set; } = null;
}
