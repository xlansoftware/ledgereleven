using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

public class UtcNullableDateTimeConverter : ValueConverter<DateTime?, DateTime?>
{
    public UtcNullableDateTimeConverter() : base(
        v => v,
        v => v.HasValue ? DateTime.SpecifyKind(v.Value, DateTimeKind.Utc) : null)
    { }
}
