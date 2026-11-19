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

        var creditService = scope.ServiceProvider
            .GetRequiredService<CreditService>();

        var batchIds = await db.ScheduledBatches
            .AsNoTracking()
            .Where(b => b.Entries.Any(e =>
                e.Status == "failed" &&
                e.CreditStatus == CreditStatus.Charged))
            .Where(b => !b.Entries.Any(e =>
                e.Status == "pending" ||
                e.Status == "processing" ||
                e.Status == "retrying"))
            .Select(b => b.Id)
            .Take(100)
            .ToListAsync(ct);

        foreach (var batchId in batchIds)
        {
            try
            {
                var refunded = await creditService
                    .RefundFailedEntriesAsync(batchId, ct);

                if (refunded > 0)
                {
                    logger.LogInformation(
                        "Refunded {Count} credits for batch {BatchId}",
                        refunded,
                        batchId);
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
                    batchId);
            }
        }
    }
}