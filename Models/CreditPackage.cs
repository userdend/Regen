
namespace Regen.Models;

public class CreditPackage
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public int Credits { get; set; }

    public decimal Amount { get; set; }

    public string Currency { get; set; } = "MYR";

    public string StripePriceId { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
