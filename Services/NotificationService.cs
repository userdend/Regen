
using Regen.Data;
using Regen.Models;

namespace Regen.Services;

public class NotificationService(
    RegenContext _db,
    ILogger<NotificationService> _logger)
{
    public async Task CreateAsync(
        Guid userId,
        string title,
        string message,
        string type = "info",
        string? actionUrl = null)
    {
        var notification = new Notification
        {
            UserId = userId,
            Title = title,
            Message = message,
            Type = type,
            ActionUrl = actionUrl,
            IsRead = false,
            CreatedAt = DateTime.UtcNow
        };

        _db.Notifications.Add(notification);

        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "Notification {NotificationId} created for user {UserId} with type {NotificationType}.",
            notification.Id,
            userId,
            type
        );
    }
}
