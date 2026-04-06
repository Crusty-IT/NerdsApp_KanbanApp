using System.Security.Claims;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Models;
using KanbanApp.Backend.Services;
using KanbanApp.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Endpoints;

public static class BoardEndpoints
{
    public static void MapBoardEndpoints(this WebApplication app)
    {
        app.MapGet("/api/boards", async (IBoardService service, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var boards = await service.GetAllByUserAsync(userId!);
            var result = boards.Select(b => new { b.Id, b.Name, b.Description, b.CreatedAt, b.ProjectId });
            return Results.Ok(result);
        }).RequireAuthorization();

        app.MapPost("/api/boards", async (CreateBoardDto dto, IBoardService boardService, ClaimsPrincipal user) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var board = await boardService.CreateAsync(dto.BoardName, null, userId!, dto.ProjectId);
            return TypedResults.Created($"/api/boards/{board.Id}", new { board.Id, board.Name, board.Description, board.ProjectId });
        }).RequireAuthorization();

        app.MapGet("/api/boards/{boardId}", async (
            int boardId, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var board = await boardService.GetByIdAsync(boardId, userId!);
            if (board == null) return Results.NotFound();

            var dto = new BoardDetailDto(
                board.Id, board.Name, board.Description, board.CreatedAt,
                board.Columns.Select(c => new ColumnDto(
                    c.Id, c.Name, c.Position, c.Color,
                    c.Cards.Select(card => new CardDto(
                        card.Id, card.Title, card.Description, card.Position, card.CreatedAt, card.AssignedToUserId, card.DueDate, card.Color
                    )).ToList()
                )).ToList()
            );
            return Results.Ok(dto);
        }).RequireAuthorization();

        app.MapGet("/api/boards/{boardId}/members", async (
            int boardId,
            IAuthorizationService authorizationService,
            ClaimsPrincipal user,
            ApplicationDbContext db) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var members = await db.BoardMembers
                .Where(m => m.BoardId == boardId)
                .Include(m => m.User)
                .Select(m => new {
                    userId = m.UserId,
                    email = m.User.Email,
                    userName = m.User.UserName,
                    role = m.Role.ToString()
                })
                .ToListAsync();

            return Results.Ok(members);
        }).RequireAuthorization();

        app.MapPut("/api/boards/{boardId}", async (
            int boardId, UpdateBoardDto dto, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var board = await boardService.UpdateAsync(boardId, userId!, dto.Name, null);
            return board is null ? Results.NotFound() : Results.Ok(new { board.Id, board.Name });
        }).RequireAuthorization();

        app.MapDelete("/api/boards/{boardId}", async (
            int boardId, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var deleted = await boardService.DeleteAsync(boardId, userId!);
            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        app.MapPost("/api/boards/{boardId}/members", async (
            int boardId, InviteMemberDto dto,
            IAuthorizationService authorizationService,
            ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var invitedUser = await db.Users.FirstOrDefaultAsync(u => u.Email == dto.Email);
            if (invitedUser == null) return Results.NotFound("User not found.");

            var alreadyMember = await db.BoardMembers
                .AnyAsync(m => m.BoardId == boardId && m.UserId == invitedUser.Id);
            if (alreadyMember) return Results.Conflict("User is already a member of this board.");

            var member = new BoardMember { BoardId = boardId, UserId = invitedUser.Id, Role = BoardRole.Member };
            db.BoardMembers.Add(member);
            await db.SaveChangesAsync();
            return Results.Ok(new { invitedUser.Email, boardId });
        }).RequireAuthorization();
    }
}