using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regen.Data;

namespace Regen.Controllers;

public class DashboardsController(RegenContext db) : Controller
{
  public IActionResult Index() => View();
  public IActionResult CRM() => View();

  [HttpGet]
  public async Task<IActionResult> GetStats()
  {
    var userId = Guid.Parse(
        User.FindFirst("RegenUserId")!.Value
    );

    var totalBatches = await db.ScheduledBatches
        .AsNoTracking()
        .CountAsync(b => b.UserId == userId);

    var stats = await db.ScheduledEntries
        .AsNoTracking()
        .Where(e => e.Batch.UserId == userId)
        .GroupBy(e => e.Status)
        .Select(g => new
        {
          Status = g.Key,
          Count = g.Count()
        })
        .ToListAsync();

    int Count(string status) =>
        stats.FirstOrDefault(s => s.Status == status)?.Count ?? 0;

    var completed = Count("completed");
    var pending = Count("pending");
    var retrying = await db.ScheduledEntries
      .AsNoTracking()
      .CountAsync(e =>
          e.Batch.UserId == userId &&
          e.Status == "retrying" &&
          e.RetryCount > 0
      );
    var failed = Count("failed");

    var totalEntries = stats.Sum(s => s.Count);

    double Percentage(int count) =>
        totalEntries == 0
            ? 0
            : Math.Round(count * 100.0 / totalEntries, 2);


    var now = DateTime.UtcNow;

    // Last 24 complete hourly buckets, including current hour.
    var currentHour = new DateTime(
        now.Year, now.Month, now.Day, now.Hour,
        0, 0, DateTimeKind.Utc
    );

    var startHour = currentHour.AddHours(-23);
    var endHour = currentHour.AddHours(1);

    var hourlyStats = await db.ScheduledEntries
        .AsNoTracking()
        .Where(e =>
            e.Batch.UserId == userId &&
            e.Status == "completed" &&
            e.ProcessedAt >= startHour &&
            e.ProcessedAt < endHour
        )
        .GroupBy(e => e.ProcessedAt!.Value.Hour)
        .Select(g => new
        {
          Hour = g.Key,
          Count = g.Count()
        })
        .ToListAsync();

    var hourlyLookup = hourlyStats.ToDictionary(
        x => x.Hour,
        x => x.Count
    );

    var hours = Enumerable.Range(0, 24)
        .Select(i => startHour.AddHours(i))
        .ToList();

    var hourlyOverview = new
    {
      labels = hours.Select(h => h.ToString("HH:mm")).ToArray(),
      data = hours.Select(h =>
          hourlyLookup.GetValueOrDefault(h.Hour)
        ).ToArray(),
      totalCompleted = hourlyStats.Sum(x => x.Count)
    };


    return Ok(new
    {
      totalBatches,
      totalEntries,
      completed = new
      {
        count = completed,
        percentage = Percentage(completed)
      },
      pending = new
      {
        count = pending,
        percentage = Percentage(pending),
        retrying = retrying
      },
      failed = new
      {
        count = failed,
        percentage = Percentage(failed)
      },
      hourlyOverview = new
      {
        data = Enumerable.Range(0, 6)
        .Select(i => new
        {
          x = DateTime.UtcNow
                .AddHours(i - 5)
                .ToString("yyyy-MM-ddTHH:mm:ssZ"),
          y = new[] { 12, 25, 18, 42, 35, 10 }[i]
        })
        .ToArray(),

        totalCompleted = 142
      }
    });
  }
}
