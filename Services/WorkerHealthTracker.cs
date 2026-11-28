using System.Collections.Concurrent;

namespace Regen.Services;

public class WorkerHealthTracker
{
    private readonly ConcurrentDictionary<string, DateTime> _heartbeats = new();

    public void ReportHeartbeat(string workerName)
    {
        _heartbeats[workerName] = DateTime.UtcNow;
    }

    public bool TryGetLastHeartbeat(
        string workerName,
        out DateTime lastHeartbeat)
    {
        return _heartbeats.TryGetValue(workerName, out lastHeartbeat);
    }
}