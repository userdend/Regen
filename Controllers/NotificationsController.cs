
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Regen.Data;
using System.Security.Claims;

namespace Regen.Controllers;

[Authorize]
[ApiController]
[Route("api/notifications")]
public class NotificationsController(
    RegenContext _db) : ControllerBase
{
    private bool TryGetUserId(out Guid userId)
    {
        return Guid.TryParse(
            User.FindFirstValue("RegenUserId"),
            out userId
        );
    }

    // GET: /api/notifications
    [HttpGet]
    public async Task<IActionResult> GetNotifications(CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notifications = await _db.Notifications
            .AsNoTracking()
            .Where(n => n.UserId == userId)
            .OrderByDescending(n => n.CreatedAt)
            .Take(20)
            .Select(n => new
            {
                n.Id,
                n.Title,
                n.Message,
                n.Type,
                n.IsRead,
                n.ActionUrl,
                n.CreatedAt
            })
            .ToListAsync(ct);

        var unreadCount = await _db.Notifications
            .CountAsync(n => n.UserId == userId && !n.IsRead, ct);

        return Ok(new
        {
            notifications,
            unreadCount
        });
    }

    // POST: /api/notifications/{id}/read
    [HttpPost("{id:int}/read")]
    public async Task<IActionResult> MarkAsRead(
        int id,
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        var notification = await _db.Notifications
            .FirstOrDefaultAsync(
                n => n.Id == id && n.UserId == userId,
                ct);

        if (notification == null)
            return NotFound();

        notification.IsRead = true;

        await _db.SaveChangesAsync(ct);

        return NoContent();
    }

    // POST: /api/notifications/read-all
    [HttpPost("read-all")]
    public async Task<IActionResult> MarkAllAsRead(
        CancellationToken ct)
    {
        if (!TryGetUserId(out var userId))
            return Unauthorized();

        await _db.Notifications
            .Where(n => n.UserId == userId && !n.IsRead)
            .ExecuteUpdateAsync(
                setters => setters.SetProperty(
                    n => n.IsRead, true),
                ct);

        return NoContent();
    }
}
