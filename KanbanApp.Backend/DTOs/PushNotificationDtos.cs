namespace KanbanApp.Backend.DTOs;

public record PushKeysDto(string P256dh, string Auth);

public record SavePushSubscriptionDto(
    string Endpoint,
    PushKeysDto Keys,
    string? UserAgent = null);

public record DeletePushSubscriptionDto(string? Endpoint = null);

public record PushConfigDto(bool IsConfigured, string? PublicKey);

public record NotificationPreferencesDto(
    bool WebPushEnabled,
    bool NotifyCardCreated,
    bool NotifyCardUpdated,
    bool NotifyCardMoved,
    bool NotifyCardDeleted,
    bool NotifyCardAssigned,
    bool IncludeCardDetails);

public record UpdateNotificationPreferencesDto(
    bool? WebPushEnabled = null,
    bool? NotifyCardCreated = null,
    bool? NotifyCardUpdated = null,
    bool? NotifyCardMoved = null,
    bool? NotifyCardDeleted = null,
    bool? NotifyCardAssigned = null,
    bool? IncludeCardDetails = null);
