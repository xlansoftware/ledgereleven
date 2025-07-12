namespace ledger11.model.Api;

public class SpaceDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public Dictionary<string, string?> Settings { get; set; } = new();

    public decimal? TotalValue { get; set; } = null;
    public int? CountTransactions { get; set; } = null;
    public int? CountCategories { get; set; } = null;
    public List<string?> Members { get; set; } = [];
}
