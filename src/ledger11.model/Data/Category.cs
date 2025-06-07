namespace ledger11.model.Data;

public class Category
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int DisplayOrder { get; set; }
    public string? Color { get; set; }
    public string? Icon { get; set; }   // if you want to show icons in the UI
}
