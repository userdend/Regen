using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Models.Enums;

namespace Regen.Services;

public class CreditRefundWorker(
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
        await CheckRefunds(stoppingToken);

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
            try
            {
                var refunded = await creditService
                    .RefundFailedEntriesAsync(batch.Id, ct);

                if (refunded > 0)
                {
                    logger.LogInformation(
                        "Refunded {Count} credits for batch {batchId}",
                        refunded,
                        batch.Id);

                    await notificationService.CreateAsync(
                        batch.UserId,
                        "Credits refunded",
                        $"{refunded} credits have been refunded to your account.",
                        "success",
                        "/Dashboards/Index"
                    );
                }
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
                    "Failed to refund batch {BatchId}",
                    batch.Id);
            }
        }
    }
}