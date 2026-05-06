namespace KanbanApp.Backend.Models;

public class ProjectMember
{
    public string UserId { get; set; } = string.Empty;
    public int ProjectId { get; set; }
    public ApplicationUser User { get; set; } = null!;
    public Project Project { get; set; } = null!;
    public ProjectRole Role { get; set; } = ProjectRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}

public enum ProjectRole { Member, Owner }