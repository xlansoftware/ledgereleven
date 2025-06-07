using System.ComponentModel.DataAnnotations;

namespace ledger11.model.Data;

public class WaitlistEntry
{
    public int Id { get; set; }

    [Required, EmailAddress]
    public string Email { get; set; } = string.Empty;

    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}