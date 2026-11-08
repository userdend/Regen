namespace Regen.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string GoogleId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;

    public string? AvatarUrl { get; set; }

    public string? StripeCustomerId { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
}