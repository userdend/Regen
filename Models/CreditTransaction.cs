namespace Regen.Models;

public class CreditTransaction
{
    public Guid Id { get; set; } = Guid.NewGuid();

    public Guid UserId { get; set; }
    public Guid? BatchId { get; set; }
    public Guid? EntryId { get; set; }

    public int Amount { get; set; }

    public string Type { get; set; } = string.Empty;

    public string OperationKey { get; set; } = string.Empty;

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}