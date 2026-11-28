using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace Regen.Services;

public class WorkerHealthCheck : IHealthCheck
{
    private readonly WorkerHealthTracker _tracker;

    public WorkerHealthCheck(WorkerHealthTracker tracker)
    {
        _tracker = tracker;
    }

    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var workers = new[]
        {
            nameof(ScheduleWorker),
            nameof(CreditRefundWorker)
        };

        foreach (var worker in workers)
        {
            if (!_tracker.TryGetLastHeartbeat(
                worker, out var lastHeartbeat))
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        $"{worker} has not reported a heartbeat."));
            }

            if (DateTime.UtcNow - lastHeartbeat >
                TimeSpan.FromMinutes(2))
            {
                return Task.FromResult(
                    HealthCheckResult.Unhealthy(
                        $"{worker} heartbeat is stale."));
            }
        }

        return Task.FromResult(
            HealthCheckResult.Healthy("All workers are healthy."));
    }
}