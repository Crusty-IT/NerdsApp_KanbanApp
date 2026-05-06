using Microsoft.AspNetCore.Identity;

namespace KanbanApp.Backend.Models;

public class ApplicationUser : IdentityUser
{
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
    public ICollection<ProjectMember> ProjectMembers { get; set; } = new List<ProjectMember>();
}