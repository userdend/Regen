using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regen.Data;

namespace Regen.Controllers;

[Authorize]
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
    var failed = Count("failed");

    var retrying = await db.ScheduledEntries
        .AsNoTracking()
        .CountAsync(e =>
            e.Batch.UserId == userId &&
            e.Status == "retrying" &&
            e.RetryCount > 0
        );

    var totalEntries = stats.Sum(s => s.Count);

    double Percentage(int count) =>
        totalEntries == 0
            ? 0
            : Math.Round(count * 100.0 / totalEntries, 2);

    // Sample chart data: 12 hours, 5-minute intervals
    var random = new Random();
    var now = DateTime.UtcNow;

    var end = new DateTime(
        now.Year,
        now.Month,
        now.Day,
        now.Hour,
        (now.Minute / 5) * 5,
        0,
        DateTimeKind.Utc
    );

    var data = Enumerable.Range(0, 144)
        .Select(i =>
        {
          var chance = random.NextDouble();

          var count = chance switch
          {
            < 0.50 => 0,
            < 0.85 => random.Next(1, 6),
            < 0.97 => random.Next(6, 20),
            _ => random.Next(20, 100)
          };

          return new
          {
            x = end.AddMinutes((i - 143) * 5)
                  .ToString("yyyy-MM-ddTHH:mm:ssZ"),
            y = count
          };
        })
        .ToArray();

    var hourlyOverview = new
    {
      data,
      totalCompleted = data.Sum(x => x.y)
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
        retrying
      },
      failed = new
      {
        count = failed,
        percentage = Percentage(failed)
      },
      hourlyOverview
    });
  }

  [HttpGet]
  public async Task<IActionResult> GetFailedBatches()
  {
    var userId = Guid.Parse(
        User.FindFirst("RegenUserId")!.Value
    );

    var batches = await db.ScheduledBatches
        .AsNoTracking()
        .Where(b => b.UserId == userId)

        // At least one failed entry
        .Where(b => b.Entries.Any(e =>
            e.Status == "failed"))

        // No pending entries
        .Where(b => !b.Entries.Any(e =>
            e.Status == "pending"))

        .OrderByDescending(b => b.CreatedAt)
        .Select(b => new
        {
          b.Id,
          b.CreatedAt,
          TotalEntries = b.Entries.Count(),
          FailedEntries = b.Entries.Count(e =>
                  e.Status == "failed"),
          CompletedEntries = b.Entries.Count(e =>
                  e.Status == "completed")
        })
        .ToListAsync();

    return Json(batches);
  }

  [HttpPost]
  [ValidateAntiForgeryToken]
  public async Task<IActionResult> RetryBatch(Guid batchId)
  {
    var userId = Guid.Parse(
        User.FindFirst("RegenUserId")!.Value
    );

    await using var transaction = await db.Database.BeginTransactionAsync();

    // Lock the batch until this transaction completes.
    var batch = await db.ScheduledBatches
        .FromSqlInterpolated($"""
            SELECT *
            FROM "ScheduledBatches"
            WHERE "Id" = {batchId}
              AND "UserId" = {userId}
            FOR UPDATE
            """)
        .SingleOrDefaultAsync();

    if (batch == null)
    {
      return NotFound(new
      {
        message = "Batch not found."
      });
    }

    var hasPending = await db.ScheduledEntries
        .AnyAsync(e =>
            e.BatchId == batchId &&
            e.Status == "pending");

    if (hasPending)
    {
      return Conflict(new
      {
        message = "Batch still contains pending entries."
      });
    }

    var now = DateTime.UtcNow;

    var updatedCount = await db.ScheduledEntries
        .Where(e => e.BatchId == batchId)
        .Where(e => e.Status == "failed")
        .ExecuteUpdateAsync(setters => setters
            .SetProperty(e => e.Status, "pending")
            .SetProperty(e => e.ScheduledAt, now)
            .SetProperty(e => e.ProcessedAt, (DateTime?)null)
            .SetProperty(e => e.LastError, (string?)null)
        );

    if (updatedCount == 0)
    {
      return Conflict(new
      {
        message = "No failed entries available for retry."
      });
    }

    await transaction.CommitAsync();

    return Ok(new
    {
      message = "Batch queued for retry.",
      batchId,
      retriedEntries = updatedCount
    });
  }
}
