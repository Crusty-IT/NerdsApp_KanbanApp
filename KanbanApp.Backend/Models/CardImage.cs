namespace KanbanApp.Backend.Models;

public class CardImage
{
    public int Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public string? ObjectPosition { get; set; }
    public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    public int CardId { get; set; }
    public Card Card { get; set; } = null!;
    public string UploadedByUserId { get; set; } = string.Empty;
}
