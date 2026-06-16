using System.Security.Claims;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Hubs;
using KanbanApp.Backend.Models;
using KanbanApp.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Endpoints;

public static class ColumnEndpoints
{
    public static void MapColumnEndpoints(this WebApplication app)
    {
        var columns = app.MapGroup("/api/boards/{boardId}/columns")
            .RequireAuthorization();

        columns.MapPost("/", async (int boardId, CreateColumnDto dto, ApplicationDbContext db,
            IAuthorizationService authorizationService, ClaimsPrincipal user,
            IHubContext<KanbanHub> hubContext) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Column name cannot be empty.");
            if (dto.Name.Length > 100)
                return Results.BadRequest("Column name cannot exceed 100 characters.");

            var position = dto.Position ?? await db.Columns.Where(c => c.BoardId == boardId).CountAsync();
            var column = new Column
            {
                Name = dto.Name,
                Position = position,
                BoardId = boardId,
                Color = dto.Color ?? "#00d4ff"
            };
            db.Columns.Add(column);
            await db.SaveChangesAsync();

            var columnPayload = new { column.Id, column.Name, column.Position, column.Color };
            await hubContext.Clients.Group($"board-{boardId}").SendAsync("ColumnCreated", new { column = columnPayload });

            return TypedResults.Created($"/api/boards/{boardId}/columns/{column.Id}", columnPayload);
        });

        columns.MapPut("/{columnId}", async (int boardId, int columnId, UpdateColumnDto dto, ApplicationDbContext db,
            IAuthorizationService authorizationService, ClaimsPrincipal user,
            IHubContext<KanbanHub> hubContext) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Column name cannot be empty.");
            if (dto.Name.Length > 100)
                return Results.BadRequest("Column name cannot exceed 100 characters.");

            var column = await db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (column == null) return Results.NotFound();
            column.Name = dto.Name;
            if (dto.Color != null) column.Color = dto.Color;
            if (dto.Position.HasValue) column.Position = dto.Position.Value;
            await db.SaveChangesAsync();

            var movedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var columnPayload = new { column.Id, column.Name, column.Position, column.Color };
            await hubContext.Clients.Group($"board-{boardId}").SendAsync("ColumnUpdated", new { column = columnPayload, movedByUserId });

            return Results.Ok(columnPayload);
        });

        // S2: scope to boardId to prevent cross-board card deletion
        // C1: clean up card image files from disk
        columns.MapDelete("/{columnId}/cards", async (int boardId, int columnId, ApplicationDbContext db,
            IAuthorizationService authorizationService, ClaimsPrincipal user, IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var columnExists = await db.Columns.AnyAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (!columnExists) return Results.NotFound();

            var cards = await db.Cards
                .Include(c => c.Images)
                .Where(c => c.ColumnId == columnId)
                .ToListAsync();

            db.Cards.RemoveRange(cards);
            await db.SaveChangesAsync();

            foreach (var card in cards)
                foreach (var image in card.Images)
                    CoverImageHelper.DeleteLocalImage(env, image.Url);

            return Results.NoContent();
        });

        columns.MapDelete("/{columnId}", async (int boardId, int columnId, ApplicationDbContext db,
            IAuthorizationService authorizationService, ClaimsPrincipal user,
            IHubContext<KanbanHub> hubContext) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var column = await db.Columns.Include(c => c.Cards)
                .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
            if (column == null) return Results.NotFound();
            if (column.Cards.Any()) return Results.BadRequest("Cannot delete column with existing cards.");
            db.Columns.Remove(column);
            await db.SaveChangesAsync();

            await hubContext.Clients.Group($"board-{boardId}").SendAsync("ColumnDeleted", new { columnId });

            return Results.NoContent();
        });
    }
}
