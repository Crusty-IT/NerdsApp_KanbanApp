namespace KanbanApp.Backend.Models;

public class NotificationPreference
{
    public string UserId { get; set; } = string.Empty;
    public bool WebPushEnabled { get; set; } = true;
    public bool NotifyCardCreated { get; set; } = true;
    public bool NotifyCardUpdated { get; set; } = true;
    public bool NotifyCardMoved { get; set; } = true;
    public bool NotifyCardDeleted { get; set; } = true;
    public bool NotifyCardAssigned { get; set; } = true;
    public bool IncludeCardDetails { get; set; } = true;
    public ApplicationUser User { get; set; } = null!;
}
