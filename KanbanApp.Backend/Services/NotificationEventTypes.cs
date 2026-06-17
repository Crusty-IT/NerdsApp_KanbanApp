namespace KanbanApp.Backend.Services;

public static class NotificationEventTypes
{
    public const string CardCreated = "card-created";
    public const string CardUpdated = "card-updated";
    public const string CardMoved = "card-moved";
    public const string CardDeleted = "card-deleted";
    public const string CardAssigned = "card-assigned";
}
