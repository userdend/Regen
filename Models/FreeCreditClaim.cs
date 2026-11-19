namespace Regen.Models;

public class FreeCreditClaim
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public string GoogleIdHash { get; set; } = string.Empty;

    public DateTime ClaimedAt { get; set; } = DateTime.UtcNow;
}