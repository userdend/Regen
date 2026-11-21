namespace Regen.Models;

public class StripeWebhookEvent
{
    public string Id { get; set; } = string.Empty;

    public string Type { get; set; } = string.Empty;

    public DateTime ProcessedAt { get; set; }
}
