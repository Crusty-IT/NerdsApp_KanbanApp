using System.Security.Claims;
using KanbanApp.Backend.Data;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Models;
using KanbanApp.Backend.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Endpoints;

public static class PushNotificationEndpoints
{
    public static void MapPushNotificationEndpoints(this WebApplication app)
    {
        app.MapGet("/api/push/config", (IPushNotificationService pushService) =>
            Results.Ok(new PushConfigDto(pushService.IsConfigured, pushService.PublicKey)));

        app.MapPost("/api/push/subscriptions", async (
            SavePushSubscriptionDto dto,
            ClaimsPrincipal user,
            HttpContext http,
            ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            if (string.IsNullOrWhiteSpace(dto.Endpoint) ||
                string.IsNullOrWhiteSpace(dto.Keys.P256dh) ||
                string.IsNullOrWhiteSpace(dto.Keys.Auth))
            {
                return Results.BadRequest(new { message = "Invalid push subscription." });
            }

            var subscription = await db.PushSubscriptions
                .FirstOrDefaultAsync(s => s.Endpoint == dto.Endpoint);

            if (subscription == null)
            {
                subscription = new PushSubscription
                {
                    Endpoint = dto.Endpoint,
                    CreatedAt = DateTime.UtcNow
                };
                db.PushSubscriptions.Add(subscription);
            }

            subscription.UserId = userId;
            subscription.P256dh = dto.Keys.P256dh;
            subscription.Auth = dto.Keys.Auth;
            subscription.UserAgent = dto.UserAgent ?? http.Request.Headers.UserAgent.ToString();
            subscription.IsActive = true;
            subscription.FailedAttempts = 0;
            subscription.RevokedAt = null;
            subscription.UpdatedAt = DateTime.UtcNow;

            await EnsurePreferencesAsync(db, userId);
            await db.SaveChangesAsync();

            return Results.Ok(new { subscription.Id, subscription.IsActive });
        }).RequireAuthorization();

        app.MapDelete("/api/push/subscriptions", async (
            [FromBody] DeletePushSubscriptionDto? dto,
            ClaimsPrincipal user,
            ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var query = db.PushSubscriptions.Where(s => s.UserId == userId && s.IsActive);

            if (!string.IsNullOrWhiteSpace(dto?.Endpoint))
                query = query.Where(s => s.Endpoint == dto.Endpoint);

            var subscriptions = await query.ToListAsync();
            foreach (var subscription in subscriptions)
            {
                subscription.IsActive = false;
                subscription.RevokedAt = DateTime.UtcNow;
                subscription.UpdatedAt = DateTime.UtcNow;
            }

            await db.SaveChangesAsync();
            return Results.Ok(new { removed = subscriptions.Count });
        }).RequireAuthorization();

        app.MapGet("/api/notifications/preferences", async (
            ClaimsPrincipal user,
            ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var preferences = await EnsurePreferencesAsync(db, userId);
            await db.SaveChangesAsync();
            return Results.Ok(ToDto(preferences));
        }).RequireAuthorization();

        app.MapPut("/api/notifications/preferences", async (
            UpdateNotificationPreferencesDto dto,
            ClaimsPrincipal user,
            ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var preferences = await EnsurePreferencesAsync(db, userId);

            if (dto.WebPushEnabled.HasValue) preferences.WebPushEnabled = dto.WebPushEnabled.Value;
            if (dto.NotifyCardCreated.HasValue) preferences.NotifyCardCreated = dto.NotifyCardCreated.Value;
            if (dto.NotifyCardUpdated.HasValue) preferences.NotifyCardUpdated = dto.NotifyCardUpdated.Value;
            if (dto.NotifyCardMoved.HasValue) preferences.NotifyCardMoved = dto.NotifyCardMoved.Value;
            if (dto.NotifyCardDeleted.HasValue) preferences.NotifyCardDeleted = dto.NotifyCardDeleted.Value;
            if (dto.NotifyCardAssigned.HasValue) preferences.NotifyCardAssigned = dto.NotifyCardAssigned.Value;
            if (dto.IncludeCardDetails.HasValue) preferences.IncludeCardDetails = dto.IncludeCardDetails.Value;

            await db.SaveChangesAsync();
            return Results.Ok(ToDto(preferences));
        }).RequireAuthorization();
    }

    private static async Task<NotificationPreference> EnsurePreferencesAsync(ApplicationDbContext db, string userId)
    {
        var preferences = await db.NotificationPreferences.FindAsync(userId);
        if (preferences != null) return preferences;

        preferences = new NotificationPreference { UserId = userId };
        db.NotificationPreferences.Add(preferences);
        return preferences;
    }

    private static NotificationPreferencesDto ToDto(NotificationPreference preferences) => new(
        preferences.WebPushEnabled,
        preferences.NotifyCardCreated,
        preferences.NotifyCardUpdated,
        preferences.NotifyCardMoved,
        preferences.NotifyCardDeleted,
        preferences.NotifyCardAssigned,
        preferences.IncludeCardDetails);
}
