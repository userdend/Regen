namespace Regen.Models;

public class ScheduledEntry
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid BatchId { get; set; }

    public ScheduledBatch Batch { get; set; } = null!;

    public int Sequence { get; set; }

    public string Url { get; set; } = string.Empty;

    public DateTime ScheduledAt { get; set; }

    public string Status { get; set; } = "pending";

    public DateTime? ProcessedAt { get; set; }

    public string? LastError { get; set; }
}