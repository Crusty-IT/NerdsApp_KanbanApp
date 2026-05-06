using System.Security.Claims;
using KanbanApp.Backend.Data;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Endpoints;

public static class ProjectEndpoints
{
    public static void MapProjectEndpoints(this WebApplication app)
    {
        app.MapGet("/api/projects", async (ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await db.Projects
                .Where(p => p.OwnerId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => new
                {
                    p.Id, p.Name, p.Description, p.Color, p.CreatedAt,
                    IsOwner = p.OwnerId == userId
                })
                .ToListAsync();
            return Results.Ok(projects);
        }).RequireAuthorization();

        app.MapPost("/api/projects", async (CreateProjectDto dto, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                Color = dto.Color ?? "#00d4ff",
                OwnerId = userId!
            };
            db.Projects.Add(project);
            await db.SaveChangesAsync();
            return Results.Created($"/api/projects/{project.Id}", new { project.Id, project.Name, project.Color });
        }).RequireAuthorization();

        app.MapGet("/api/projects/{projectId}", async (int projectId, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await db.Projects
                .Include(p => p.Boards)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId &&
                    (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)));
            if (project == null) return Results.NotFound();
            return Results.Ok(new
            {
                project.Id, project.Name, project.Description, project.Color, project.CreatedAt,
                IsOwner = project.OwnerId == userId,
                Boards = project.Boards.Select(b => new { b.Id, b.Name, b.Description, b.Color, b.CreatedAt })
            });
        }).RequireAuthorization();

        app.MapPut("/api/projects/{projectId}", async (int projectId, UpdateProjectDto dto, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            project.Name = dto.Name;
            project.Description = dto.Description;
            project.Color = dto.Color ?? project.Color;
            await db.SaveChangesAsync();
            return Results.Ok(new { project.Id, project.Name, project.Color });
        }).RequireAuthorization();

        app.MapDelete("/api/projects/{projectId}", async (int projectId, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await db.Projects
                .Include(p => p.Boards).ThenInclude(b => b.Columns).ThenInclude(c => c.Cards)
                .Include(p => p.Boards).ThenInclude(b => b.BoardMembers)
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            foreach (var board in project.Boards)
            {
                db.Cards.RemoveRange(board.Columns.SelectMany(c => c.Cards));
                db.Columns.RemoveRange(board.Columns);
                db.BoardMembers.RemoveRange(board.BoardMembers);
            }
            db.Boards.RemoveRange(project.Boards);
            db.ProjectMembers.RemoveRange(project.Members);
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();

        app.MapGet("/api/projects/{projectId}/members", async (int projectId, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAccess = await db.Projects.AnyAsync(p => p.Id == projectId &&
                (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!hasAccess) return Results.NotFound();
            var members = await db.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .Include(m => m.User)
                .Select(m => new ProjectMemberDetailDto(
                    m.UserId, m.User.UserName!, m.User.ProfilePictureUrl,
                    m.Role.ToString(), m.JoinedAt))
                .ToListAsync();
            return Results.Ok(members);
        }).RequireAuthorization();

        app.MapPost("/api/projects/{projectId}/members", async (int projectId, InviteProjectMemberDto dto, ApplicationDbContext db, ClaimsPrincipal user, UserManager<ApplicationUser> userManager) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            var invitedUser = await userManager.FindByEmailAsync(dto.Email);
            if (invitedUser == null) return Results.BadRequest(new { message = "User not found" });
            if (invitedUser.Id == userId) return Results.BadRequest(new { message = "Cannot invite yourself" });
            var alreadyMember = await db.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == invitedUser.Id);
            if (alreadyMember) return Results.BadRequest(new { message = "User is already a member" });
            db.ProjectMembers.Add(new ProjectMember { ProjectId = projectId, UserId = invitedUser.Id });
            await db.SaveChangesAsync();
            return Results.Ok(new { message = "Member invited successfully" });
        }).RequireAuthorization();

        app.MapDelete("/api/projects/{projectId}/members/{memberId}", async (int projectId, string memberId, ApplicationDbContext db, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            var member = await db.ProjectMembers.FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberId);
            if (member == null) return Results.NotFound();
            db.ProjectMembers.Remove(member);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}