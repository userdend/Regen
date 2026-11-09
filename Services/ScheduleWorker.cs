using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models;

namespace Regen.Services;

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
        var now = DateTime.UtcNow;

        var db = scope.ServiceProvider
            .GetRequiredService<RegenContext>();

        // Get the earliest due entry.
        var entry = await db.ScheduledEntries
            .AsNoTracking()
            .Where(e =>
                (e.Status == "pending" && e.ScheduledAt <= now) ||
                (
                    e.Status == "retrying" &&
                    e.NextRetryAt != null &&
                    e.NextRetryAt <= now
                )
            )
            .OrderBy(e => e.Status == "retrying" ? e.NextRetryAt : e.ScheduledAt)
            .ThenBy(e => e.Sequence)
            .FirstOrDefaultAsync(ct);

        if (entry is null)
            return;

        // var userId = entry.Batch.UserId;

        // Claim the entry before processing.
        var claimed = await db.ScheduledEntries
            .Where(e =>
                e.Id == entry.Id && (e.Status == "pending" || e.Status == "retrying"))
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

            await ProcessEntry(db, entry, ct);

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

    private async Task ProcessEntry(
    RegenContext db,
    ScheduledEntry entry,
    CancellationToken ct)
    {
        const int maxRetries = 3;
        const int baseDelaySeconds = 30;

        try
        {
            // Prototype: simulate an HTTP 500 response.
            await Task.Delay(500, ct);

            var statusCode = 500;

            if (statusCode == 500 && entry.RetryCount < maxRetries)
            {
                var retryNumber = entry.RetryCount + 1;

                var delaySeconds = baseDelaySeconds *
                    (int)Math.Pow(2, retryNumber - 1);

                var nextRetryAt = DateTime.UtcNow
                    .AddSeconds(delaySeconds);

                await db.ScheduledEntries
                    .Where(e =>
                        e.Id == entry.Id &&
                        e.Status == "processing")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.Status, "retrying")
                        .SetProperty(e => e.RetryCount, retryNumber)
                        .SetProperty(e => e.NextRetryAt, nextRetryAt)
                        .SetProperty(e => e.LastHttpStatusCode, statusCode)
                        .SetProperty(e => e.LastError, "HTTP 500"),
                        ct);

                logger.LogWarning(
                    "Entry {EntryId} returned HTTP 500. Retry {Retry}/3 at {NextRetry}",
                    entry.Id,
                    retryNumber,
                    nextRetryAt
                );

                return;
            }

            if (statusCode >= 200 && statusCode < 300)
            {
                await db.ScheduledEntries
                    .Where(e =>
                        e.Id == entry.Id &&
                        e.Status == "processing")
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(e => e.Status, "completed")
                        .SetProperty(e => e.ProcessedAt, DateTime.UtcNow)
                        .SetProperty(e => e.NextRetryAt, (DateTime?)null)
                        .SetProperty(e => e.LastError, (string?)null)
                        .SetProperty(e => e.LastHttpStatusCode, statusCode),
                        ct);

                return;
            }

            // Non-retryable error or retries exhausted.
            await db.ScheduledEntries
                .Where(e =>
                    e.Id == entry.Id &&
                    e.Status == "processing")
                .ExecuteUpdateAsync(s => s
                    .SetProperty(e => e.Status, "failed")
                    .SetProperty(e => e.NextRetryAt, (DateTime?)null)
                    .SetProperty(e => e.LastHttpStatusCode, statusCode)
                    .SetProperty(e => e.LastError, $"HTTP {statusCode}"),
                    ct);

            logger.LogError(
                "Entry {EntryId} permanently failed with HTTP {StatusCode}",
                entry.Id,
                statusCode
            );
        }
        catch (OperationCanceledException)
            when (ct.IsCancellationRequested)
        {
            throw;
        }
    }
}