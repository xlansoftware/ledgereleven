using System.Text.Json.Serialization;

namespace ledger11.model.Api;

public class Receipt
{
    [JsonPropertyName("items")]
    public List<Item> Items { get; set; } = new List<Item>();

    [JsonPropertyName("total_paid")]
    public string TotalPaid { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;
}

public class Item
{
    [JsonPropertyName("name")]
    public string Name { get; set; } = string.Empty;

    [JsonPropertyName("quantity")]
    public string Quantity { get; set; } = string.Empty;

    [JsonPropertyName("unit_price")]
    public string UnitPrice { get; set; } = string.Empty;

    [JsonPropertyName("total_price")]
    public string TotalPrice { get; set; } = string.Empty;

    [JsonPropertyName("category")]
    public string Category { get; set; } = string.Empty;

}
