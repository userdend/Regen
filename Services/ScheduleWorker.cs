using Microsoft.EntityFrameworkCore;
using Regen.Data;

namespace Regen.Services;

// Strict sequential execution per batch — ensure entry 2 doesn't execute before entry 1 finishes. (Done)
// Retry mechanism — failed jobs retry automatically with exponential backoff.
// Crash recovery and idempotency — prevent duplicate processing when restarting.
// Concurrency — allow multiple independent batches to process simultaneously.
// Dashboard — show users their scheduled jobs, progress, failures, and history.

public class ScheduleWorker(
    IServiceScopeFactory scopeFactory,
    ILogger<ScheduleWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Schedule worker started.");

        // Recover unfinished prototype tasks after restart.
        using (var scope = scopeFactory.CreateScope())
        {
            var db = scope.ServiceProvider
                .GetRequiredService<RegenContext>();

            await db.ScheduledEntries
                .Where(e => e.Status == "processing")
                .ExecuteUpdateAsync(
                    s => s.SetProperty(
                        e => e.Status, "pending"),
                    stoppingToken);
        }

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(1)
        );

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessDueEntries(stoppingToken);
            }
            catch (OperationCanceledException)
                when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Schedule worker encountered an error."
                );
            }

            if (!await timer.WaitForNextTickAsync(stoppingToken))
                break;
        }
    }

    private async Task ProcessDueEntries(
        CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RegenContext>();

        // Get the earliest due entry.
        var entry = await db.ScheduledEntries
            .AsNoTracking()
            .Where(e =>
                e.Status == "pending" &&
                e.ScheduledAt <= DateTime.UtcNow &&
                !db.ScheduledEntries.Any(previous =>
                    previous.BatchId == e.BatchId &&
                    previous.Sequence < e.Sequence &&
                    previous.Status != "completed"
                )
            )
            .OrderBy(e => e.ScheduledAt)
            .ThenBy(e => e.Sequence)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return;

        // var userId = entry.Batch.UserId;

        // Claim the entry before processing.
        var claimed = await db.ScheduledEntries
            .Where(e =>
                e.Id == entry.Id &&
                e.Status == "pending")
            .ExecuteUpdateAsync(
                s => s.SetProperty(
                    e => e.Status, "processing"),
                ct);

        if (claimed == 0)
            return;

        try
        {
            logger.LogInformation(
                "Processing entry {Sequence} from batch {BatchId}",
                entry.Sequence,
                entry.BatchId
            );

            // Prototype: simulate processing.
            await Task.Delay(500, ct);

            await db.ScheduledEntries
                .Where(e => e.Id == entry.Id)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(
                            e => e.Status, "completed")
                        .SetProperty(
                            e => e.ProcessedAt,
                            DateTime.UtcNow),
                    ct);

            logger.LogInformation(
                "Entry {EntryId} completed.",
                entry.Id
            );
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            // Leave as processing for startup recovery.
            throw;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Entry {EntryId} failed.",
                entry.Id
            );

            await db.ScheduledEntries
                .Where(e => e.Id == entry.Id)
                .ExecuteUpdateAsync(
                    s => s
                        .SetProperty(
                            e => e.Status, "failed")
                        .SetProperty(
                            e => e.LastError, ex.Message),
                    ct);
        }
    }
}