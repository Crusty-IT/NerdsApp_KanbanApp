using System.Security.Claims;
using KanbanApp.Backend.Data;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Endpoints;

public static class NotificationEndpoints
{
    public static void MapNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/notifications", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notifications = await db.Notifications
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.CreatedAt)
                .Select(n => new { n.Id, n.Message, n.IsRead, n.CreatedAt, n.CardId })
                .ToListAsync();

            return Results.Ok(notifications);
        }).RequireAuthorization();

        app.MapPut("/api/notifications/{id}/read", async (int id, ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var notification = await db.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (notification == null) return Results.NotFound();

            notification.IsRead = true;
            await db.SaveChangesAsync();
            return Results.Ok(new { notification.Id, notification.IsRead });
        }).RequireAuthorization();

        app.MapPut("/api/notifications/read-all", async (ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var unread = await db.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync();

            unread.ForEach(n => n.IsRead = true);
            await db.SaveChangesAsync();
            return Results.Ok(new { updated = unread.Count });
        }).RequireAuthorization();
    }
}