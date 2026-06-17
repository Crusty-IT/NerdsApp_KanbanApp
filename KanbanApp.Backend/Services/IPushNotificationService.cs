namespace KanbanApp.Backend.Services;

public interface IPushNotificationService
{
    bool IsConfigured { get; }
    string? PublicKey { get; }
    Task SendToUserAsync(string userId, string message, int? cardId, string eventType);
}
