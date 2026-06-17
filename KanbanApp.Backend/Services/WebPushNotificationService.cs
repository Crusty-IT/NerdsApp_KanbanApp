using System.Net;
using System.Text.Json;
using KanbanApp.Backend.Data;
using Microsoft.EntityFrameworkCore;
using WebPush;

namespace KanbanApp.Backend.Services;

public class WebPushNotificationService : IPushNotificationService
{
    private readonly ApplicationDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly ILogger<WebPushNotificationService> _logger;
    private readonly WebPushClient _client = new();

    public WebPushNotificationService(
        ApplicationDbContext context,
        IConfiguration configuration,
        ILogger<WebPushNotificationService> logger)
    {
        _context = context;
        _configuration = configuration;
        _logger = logger;
    }

    public string? PublicKey => _configuration["Vapid:PublicKey"];

    private string? PrivateKey => _configuration["Vapid:PrivateKey"];

    private string Subject => _configuration["Vapid:Subject"] ?? "mailto:admin@example.com";

    public bool IsConfigured =>
        !string.IsNullOrWhiteSpace(PublicKey) &&
        !string.IsNullOrWhiteSpace(PrivateKey);

    public async Task SendToUserAsync(string userId, string message, int? cardId, string eventType)
    {
        if (!IsConfigured) return;

        var preferences = await GetPreferencesAsync(userId);
        if (!ShouldSend(preferences, eventType)) return;

        var subscriptions = await _context.PushSubscriptions
            .Where(s => s.UserId == userId && s.IsActive)
            .ToListAsync();

        if (subscriptions.Count == 0) return;

        var payload = JsonSerializer.Serialize(new
        {
            title = "Shellty.Kanban",
            body = preferences.IncludeCardDetails ? message : "You have a new Kanban notification.",
            url = cardId.HasValue ? $"/dashboard?cardId={cardId.Value}" : "/dashboard",
            eventType,
            cardId
        });

        var vapidDetails = new VapidDetails(Subject, PublicKey, PrivateKey);

        foreach (var savedSubscription in subscriptions)
        {
            var subscription = new WebPush.PushSubscription(
                savedSubscription.Endpoint,
                savedSubscription.P256dh,
                savedSubscription.Auth);

            try
            {
                await _client.SendNotificationAsync(subscription, payload, vapidDetails);
                savedSubscription.LastUsedAt = DateTime.UtcNow;
                savedSubscription.FailedAttempts = 0;
                savedSubscription.UpdatedAt = DateTime.UtcNow;
            }
            catch (WebPushException ex) when (IsGone(ex.StatusCode))
            {
                Deactivate(savedSubscription);
            }
            catch (WebPushException ex)
            {
                savedSubscription.FailedAttempts++;
                savedSubscription.UpdatedAt = DateTime.UtcNow;
                _logger.LogWarning(ex, "Web Push send failed for subscription {SubscriptionId}", savedSubscription.Id);

                if (savedSubscription.FailedAttempts >= 5)
                    Deactivate(savedSubscription);
            }
        }

        await _context.SaveChangesAsync();
    }

    private async Task<Models.NotificationPreference> GetPreferencesAsync(string userId)
    {
        var preferences = await _context.NotificationPreferences.FindAsync(userId);
        if (preferences != null) return preferences;

        preferences = new Models.NotificationPreference { UserId = userId };
        _context.NotificationPreferences.Add(preferences);
        await _context.SaveChangesAsync();
        return preferences;
    }

    private static bool ShouldSend(Models.NotificationPreference preferences, string eventType)
    {
        if (!preferences.WebPushEnabled) return false;

        return eventType switch
        {
            NotificationEventTypes.CardCreated => preferences.NotifyCardCreated,
            NotificationEventTypes.CardUpdated => preferences.NotifyCardUpdated,
            NotificationEventTypes.CardMoved => preferences.NotifyCardMoved,
            NotificationEventTypes.CardDeleted => preferences.NotifyCardDeleted,
            NotificationEventTypes.CardAssigned => preferences.NotifyCardAssigned,
            _ => true
        };
    }

    private static bool IsGone(HttpStatusCode statusCode)
        => statusCode is HttpStatusCode.Gone or HttpStatusCode.NotFound;

    private static void Deactivate(Models.PushSubscription subscription)
    {
        subscription.IsActive = false;
        subscription.RevokedAt = DateTime.UtcNow;
        subscription.UpdatedAt = DateTime.UtcNow;
    }
}
