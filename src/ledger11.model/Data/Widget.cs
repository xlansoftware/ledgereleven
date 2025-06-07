using System.Text.Json.Serialization;

namespace ledger11.model.Data;

public class Widget
{
    public int Id { get; set; }
    public string? Title { get; set; } = string.Empty;
    public string DataQuery { get; set; } = string.Empty;
    public string WidgetParamsJson { get; set; } = string.Empty;
    public int? Order { get; set; }
    public string? Definition { get; set; } = string.Empty;
}
