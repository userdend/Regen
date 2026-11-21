namespace Regen.Models;

public class CreditPurchase
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public int CreditPackageId { get; set; }
    public CreditPackage CreditPackage { get; set; } = null!;

    public string StripeSessionId { get; set; } = null!;

    public string? StripePaymentIntentId { get; set; }

    public int Credits { get; set; }

    // Stripe amount in smallest currency unit (sen)
    public long Amount { get; set; }

    public string Currency { get; set; } = "myr";

    public string Status { get; set; } = "Pending";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime? CompletedAt { get; set; }
}