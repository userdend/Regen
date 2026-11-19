namespace Regen.Services;

using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models;
using Regen.Models.Enums;

public class CreditService
{
    private readonly RegenContext _context;

    public CreditService(RegenContext context)
    {
        _context = context;
    }

    public async Task<int> RefundFailedEntriesAsync(
        Guid batchId,
        CancellationToken ct = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(ct);

        // Serialize refund and retry operations for this batch.
        var batch = await _context.ScheduledBatches
            .FromSqlInterpolated($"""
                SELECT * FROM "ScheduledBatches"
                WHERE "Id" = {batchId}
                FOR UPDATE
                """)
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (batch is null)
            throw new InvalidOperationException("Batch not found.");

        // Use the exact status values from your existing model.
        var unfinished = await _context.ScheduledEntries
            .AnyAsync(e =>
                e.BatchId == batchId &&
                (e.Status == "pending" ||
                 e.Status == "processing"), ct);

        if (unfinished)
            return 0;

        var failedEntries = await _context.ScheduledEntries
            .Where(e =>
                e.BatchId == batchId &&
                e.Status == "failed" &&
                e.CreditStatus == CreditStatus.Charged)
            .ToListAsync(ct);

        if (failedEntries.Count == 0)
            return 0;

        foreach (var entry in failedEntries)
        {
            entry.CreditStatus = CreditStatus.Refunded;

            _context.CreditTransactions.Add(new CreditTransaction
            {
                UserId = batch.UserId,
                BatchId = batchId,
                EntryId = entry.Id,
                Amount = 1,
                Type = "refund",
                OperationKey = $"refund:{entry.Id}:{entry.ChargeCycle}"
            });
        }

        var refundedCount = failedEntries.Count;

        await _context.Users
            .Where(u => u.Id == batch.UserId)
            .ExecuteUpdateAsync(s => s
                .SetProperty(
                    u => u.Credit,
                    u => u.Credit + refundedCount), ct);

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return refundedCount;
    }

    public async Task<int> RetryFailedEntriesAsync(
    Guid batchId,
    Guid userId,
    CancellationToken ct = default)
    {
        await using var transaction =
            await _context.Database.BeginTransactionAsync(ct);

        var batch = await _context.ScheduledBatches
            .FromSqlInterpolated($"""
            SELECT * FROM "ScheduledBatches"
            WHERE "Id" = {batchId}
            FOR UPDATE
            """)
            .AsNoTracking()
            .SingleOrDefaultAsync(ct);

        if (batch is null || batch.UserId != userId)
            throw new InvalidOperationException("Batch not found.");

        var hasUnfinished = await _context.ScheduledEntries
            .AnyAsync(e =>
                e.BatchId == batchId &&
                (
                    e.Status == "pending" ||
                    e.Status == "processing" ||
                    e.Status == "retrying"
                ), ct);

        if (hasUnfinished)
        {
            throw new InvalidOperationException(
                "Batch still contains unfinished entries.");
        }

        var entries = await _context.ScheduledEntries
            .Where(e =>
                e.BatchId == batchId &&
                e.Status == "failed")
            .ToListAsync(ct);

        if (entries.Count == 0)
            return 0;

        int cost = entries.Count(e =>
            e.CreditStatus == CreditStatus.Refunded);

        if (cost > 0)
        {
            var updated = await _context.Users
                .Where(u => u.Id == userId && u.Credit >= cost)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(
                        u => u.Credit,
                        u => u.Credit - cost), ct);

            if (updated == 0)
                throw new InvalidOperationException(
                    "Insufficient credits.");
        }

        foreach (var entry in entries)
        {
            if (entry.CreditStatus == CreditStatus.Refunded)
            {
                entry.ChargeCycle++;

                _context.CreditTransactions.Add(
                    new CreditTransaction
                    {
                        UserId = userId,
                        BatchId = batchId,
                        EntryId = entry.Id,
                        Amount = -1,
                        Type = "retry",
                        OperationKey =
                            $"retry:{entry.Id}:{entry.ChargeCycle}"
                    });
            }

            entry.Status = "pending";
            entry.ScheduledAt = DateTime.UtcNow;
            entry.RetryCount = 0;
            entry.NextRetryAt = null;
            entry.LastError = null;
            entry.LastHttpStatusCode = null;
            entry.ProcessedAt = null;
            entry.CreditStatus = CreditStatus.Charged;
        }

        await _context.SaveChangesAsync(ct);
        await transaction.CommitAsync(ct);

        return entries.Count;
    }
}