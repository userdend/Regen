using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regen.Data;
using Regen.Services;
using Regen.Models.Enums;
using Regen.Models.ViewModels.Admin;
using System.Text.Json;

namespace Regen.Controllers.Admin;

[Authorize]
public class AdminDashboardsController(RegenContext db, ILogger<AdminDashboardsController> logger) : Controller
{
    public async Task<IActionResult> Index()
    {
        var now = DateTime.UtcNow;
        var today = DateTime.UtcNow.Date;
        var tomorrow = today.AddDays(1);

        var totalUsers = await db.Users.CountAsync();

        var activeUsersToday = await db.Users
            .CountAsync(u =>
                u.LastLoginAt >= today &&
                u.LastLoginAt < tomorrow
            );

        var startOfMonth = new DateTime(
            now.Year,
            now.Month,
            1,
            0, 0, 0,
            DateTimeKind.Utc
        );

        var startOfNextMonth = startOfMonth.AddMonths(1);

        var newUsersThisMonth = await db.Users
            .CountAsync(u =>
                u.CreatedAt >= startOfMonth &&
                u.CreatedAt < startOfNextMonth
            );

        var deletedUsers = await db.Users
            .CountAsync(u => u.DeletedAt != null);

        var totalBatches = await db.ScheduledBatches.CountAsync();

        var successfulEntries = await db.ScheduledEntries
            .CountAsync(e => e.Status == "completed");

        var pendingEntries = await db.ScheduledEntries
            .CountAsync(e => e.Status == "pending");

        var failedEntries = await db.ScheduledEntries
            .CountAsync(e => e.Status == "failed");

        // Monday = start of week
        var startOfWeek = today.AddDays(-(((int)today.DayOfWeek + 6) % 7));
        var endOfWeek = startOfWeek.AddDays(7);

        var entries = await db.ScheduledEntries
            .Where(e =>
                e.ProcessedAt >= startOfWeek &&
                e.ProcessedAt < endOfWeek &&
                e.LastHttpStatusCode != null
            )
            .Select(e => new
            {
                StatusCode = e.LastHttpStatusCode!.Value,
                ProcessedAt = e.ProcessedAt!.Value
            })
            .ToListAsync();

        var httpStatusChart = entries
            .GroupBy(e => e.StatusCode)
            .OrderBy(g => g.Key)
            .Select(g => new HttpStatusChartViewModel
            {
                StatusCode = g.Key.ToString(),

                Data = Enumerable.Range(0, 7)
                    .Select(day => g.Count(e =>
                        ((int)e.ProcessedAt.DayOfWeek + 6) % 7 == day
                    ))
                    .ToList()
            })
            .ToList();

        var model = new DashboardViewModel
        {
            TotalUsers = totalUsers,
            ActiveUsersToday = activeUsersToday,
            NewUsersThisMonth = newUsersThisMonth,
            DeletedUsers = deletedUsers,
            TotalBatches = totalBatches,
            SuccessfulEntries = successfulEntries,
            PendingEntries = pendingEntries,
            FailedEntries = failedEntries,
            HttpStatusChart = httpStatusChart
        };

        return View(model);
    }
}
