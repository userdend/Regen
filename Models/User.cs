using Regen.Models.Enums;

namespace Regen.Models;

public class User
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string GoogleId { get; set; } = string.Empty;

    public string Email { get; set; } = string.Empty;

    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.User;
    public int Credit { get; set; } = 0;

    public string? AvatarUrl { get; set; }

    public string? StripeCustomerId { get; set; }
    public List<ScheduledBatch> ScheduledBatches { get; set; } = new();

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? LastLoginAt { get; set; }
    public DateTime? DeletedAt { get; set; }
}