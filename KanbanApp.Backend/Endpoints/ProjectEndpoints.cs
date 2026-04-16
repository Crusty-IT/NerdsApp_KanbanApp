using System.Security.Claims;
using KanbanApp.Backend.Data;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Models;
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
                .Where(p => p.OwnerId == userId)
                .Select(p => new { p.Id, p.Name, p.Description, p.Color, p.CreatedAt })
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
                .FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            return Results.Ok(new
            {
                project.Id, project.Name, project.Description, project.Color, project.CreatedAt,
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
            var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == projectId && p.OwnerId == userId);
            if (project == null) return Results.NotFound();
            db.Projects.Remove(project);
            await db.SaveChangesAsync();
            return Results.NoContent();
        }).RequireAuthorization();
    }
}