namespace KanbanApp.Backend.Models;

public class Project
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string Color { get; set; } = "#00d4ff";
    public string? CoverImageUrl { get; set; }
    public string? CoverObjectPosition { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string OwnerId { get; set; } = string.Empty;
    public ApplicationUser Owner { get; set; } = null!;
    public ICollection<Board> Boards { get; set; } = new List<Board>();
    public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
}
