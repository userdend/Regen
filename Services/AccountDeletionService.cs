using Microsoft.EntityFrameworkCore;
using Regen.Data;

namespace Regen.Services;

public class AccountDeletionService(RegenContext db)
{
    public async Task DeleteAsync(
        Guid userId,
        CancellationToken ct = default)
    {
        await using var transaction =
            await db.Database.BeginTransactionAsync(ct);

        var user = await db.Users
            .FromSqlInterpolated(
                $"""SELECT * FROM "Users" WHERE "Id" = {userId} FOR UPDATE""")
            .FirstOrDefaultAsync(ct);

        if (user == null || user.DeletedAt != null)
        {
            throw new InvalidOperationException(
                "Account not found or already deleted.");
        }

        var now = DateTime.UtcNow;

        // Cancel all unfinished scheduled entries.
        await db.ScheduledEntries
            .Where(e =>
                e.Batch.UserId == userId &&
                (e.Status == "pending" ||
                 e.Status == "retrying"))
            .ExecuteUpdateAsync(
                setters => setters
                    .SetProperty(e => e.Status, "cancelled"),
                ct);

        // Erase personal information.
        user.Email = $"deleted-{user.Id:N}@example.invalid";
        user.GoogleId = $"deleted-{user.Id:N}";
        user.Name = "Deleted User";
        user.AvatarUrl = null;
        user.StripeCustomerId = null;

        user.Credit = 0;
        user.DeletedAt = now;
        user.UpdatedAt = now;

        await db.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);
    }
}