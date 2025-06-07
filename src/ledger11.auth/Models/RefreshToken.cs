namespace ledger11.auth.Models;

using System;
using System.ComponentModel.DataAnnotations;

public class RefreshToken
{
    [Key]
    public int Id { get; set; }

    [Required]
    public string Token { get; set; } = default!;

    [Required]
    public string Username { get; set; } = default!;

    [Required]
    public string ClientId { get; set; } = default!;

    [Required]
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    [Required]
    public DateTime Expiry { get; set; }

    public bool Revoked { get; set; } = false;

    public DateTime? RevokedAt { get; set; }

    public string? ReplacedByToken { get; set; }

    public string? IpAddress { get; set; }

    public string? DeviceId { get; set; }

    // Optional: convenience method
    public bool IsActive => !Revoked && Expiry > DateTime.UtcNow;
}
