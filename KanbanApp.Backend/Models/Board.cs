namespace KanbanApp.Backend.Models;

public class Board
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#00d4ff";
    public string? CoverImageUrl { get; set; }
    public string? CoverObjectPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public int? ProjectId { get; set; }
    public Project? Project { get; set; }
    public ICollection<Column> Columns { get; set; } = new List<Column>();
    public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
}
