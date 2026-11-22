
using System.ComponentModel.DataAnnotations;

namespace Regen.Models;

public class Notification
{
    public int Id { get; set; }

    public Guid UserId { get; set; }

    [Required]
    [MaxLength(150)]
    public string Title { get; set; } = string.Empty;

    [Required]
    [MaxLength(500)]
    public string Message { get; set; } = string.Empty;

    [MaxLength(30)]
    public string Type { get; set; } = "info";

    public bool IsRead { get; set; } = false;

    [MaxLength(500)]
    public string? ActionUrl { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Navigation property
    public User? User { get; set; }
}
