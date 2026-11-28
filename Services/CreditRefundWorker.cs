using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models.Enums;

namespace Regen.Services;

public class CreditRefundWorker(
    WorkerHealthTracker tracker,
    IServiceScopeFactory scopeFactory,
    ILogger<CreditRefundWorker> logger
) : BackgroundService
{
    protected override async Task ExecuteAsync(
        CancellationToken stoppingToken)
    {
        logger.LogInformation("Credit refund worker started.");

        using var timer = new PeriodicTimer(
            TimeSpan.FromSeconds(30)
        );

        // Check immediately on startup.
        try
        {
            await CheckRefunds(stoppingToken);
            tracker.ReportHeartbeat(nameof(CreditRefundWorker));
        }
        catch (OperationCanceledException)
            when (stoppingToken.IsCancellationRequested)
        {
            return;
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Initial credit refund scan failed; continuing with scheduled scans."
            );
        }

        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            try
            {
                await CheckRefunds(stoppingToken);
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
                    "Credit refund worker encountered an error.");
            }
        }
    }

    private async Task CheckRefunds(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();

        var db = scope.ServiceProvider
            .GetRequiredService<RegenContext>();

        var notificationService = scope.ServiceProvider
            .GetRequiredService<NotificationService>();

        var creditService = scope.ServiceProvider
            .GetRequiredService<CreditService>();

        var batches = await db.ScheduledBatches
            .AsNoTracking()
            .Where(b => b.Entries.Any(e =>
                e.Status == "failed" &&
                e.CreditStatus == CreditStatus.Charged))
            .Where(b => !b.Entries.Any(e =>
                e.Status == "pending" ||
                e.Status == "processing" ||
                e.Status == "retrying"))
            .Select(b => new
            {
                b.Id,
                b.UserId
            })
            .Take(100)
            .ToListAsync(ct);

        foreach (var batch in batches)
        {
            int refunded;

            try
            {
                refunded = await creditService
                    .RefundFailedEntriesAsync(batch.Id, ct);
            }
            catch (OperationCanceledException)
                when (ct.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Failed to refund batch {BatchId} for user {UserId}.",
                    batch.Id,
                    batch.UserId
                );
                continue;
            }

            if (refunded <= 0)
                continue;

            logger.LogInformation(
                "Refunded {RefundedCredits} credits for batch {BatchId} belonging to user {UserId}.",
                refunded,
                batch.Id,
                batch.UserId
            );

            try
            {
                await notificationService.CreateAsync(
                    batch.UserId,
                    "Credits refunded",
                    $"{refunded} credits have been refunded to your account.",
                    "success",
                    "/Dashboards/Index"
                );
            }
            catch (Exception ex)
            {
                logger.LogError(
                    ex,
                    "Refund completed for batch {BatchId}, but notification creation failed for user {UserId}.",
                    batch.Id,
                    batch.UserId
                );
            }
        }
    }
}
