using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
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
            var result = boards.Select(b => new { b.Id, b.Name, b.Description, b.Color, b.CoverImageUrl, b.CoverObjectPosition, b.CreatedAt, b.ProjectId });
            return Results.Ok(result);
        }).RequireAuthorization();

        // S4: validate projectId belongs to a project the user can access
        app.MapPost("/api/boards", async (CreateBoardDto dto, IBoardService boardService,
            ClaimsPrincipal user, ApplicationDbContext db) =>
        {
            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!string.IsNullOrWhiteSpace(dto.BoardName) && dto.BoardName.Length > 100)
                return Results.BadRequest("Board name cannot exceed 100 characters.");

            if (dto.ProjectId.HasValue)
            {
                var hasAccess = await db.Projects.AnyAsync(p => p.Id == dto.ProjectId
                    && (p.OwnerId == userId || p.Members.Any(m => m.UserId == userId)));
                if (!hasAccess) return Results.Forbid();
            }

            var board = await boardService.CreateAsync(dto.BoardName, null, userId!, dto.ProjectId, dto.Color);
            return TypedResults.Created($"/api/boards/{board.Id}", new { board.Id, board.Name, board.Description, board.Color, board.CoverImageUrl, board.CoverObjectPosition, board.CreatedAt, board.ProjectId });
        }).RequireAuthorization();

        app.MapGet("/api/boards/{boardId}", async (
            int boardId, IBoardService boardService, ApplicationDbContext db,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var board = await boardService.GetByIdAsync(boardId, userId!);
            if (board == null) return Results.NotFound();

            var isBoardOwner = await db.BoardMembers
                .AnyAsync(m => m.BoardId == boardId && m.UserId == userId && m.Role == BoardRole.Owner);

            var isProjectOwner = false;
            if (board.ProjectId.HasValue)
            {
                var project = await db.Projects.FirstOrDefaultAsync(p => p.Id == board.ProjectId);
                isProjectOwner = project?.OwnerId == userId;
            }

            var dto = new BoardDetailDto(
                board.Id,
                board.Name,
                board.Description,
                board.Color,
                board.CoverImageUrl,
                board.CoverObjectPosition,
                board.CreatedAt,
                board.ProjectId,
                isBoardOwner || isProjectOwner,
                board.Columns.Select(c => new ColumnDto(
                    c.Id, c.Name, c.Position, c.Color,
                    c.Cards.Select(card => new CardDto(
                        card.Id, card.Title, card.Description, card.Position, card.ColumnId, card.CreatedAt, card.AssignedToUserId, card.DueDate, card.Priority,
                        card.Images.Select(i => new CardImageDto(i.Id, i.FileName, i.Url, i.ContentType, i.ObjectPosition, i.UploadedAt)).ToList()
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
                    profilePictureUrl = m.User.ProfilePictureUrl,
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

            if (string.IsNullOrWhiteSpace(dto.Name))
                return Results.BadRequest("Board name cannot be empty.");
            if (dto.Name.Length > 100)
                return Results.BadRequest("Board name cannot exceed 100 characters.");

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var board = await boardService.UpdateAsync(boardId, userId!, dto.Name, dto.Description, dto.Color);
            if (board is null) return Results.NotFound();
            return Results.Ok(new { board.Id, board.Name, board.Color, board.CoverImageUrl, board.CoverObjectPosition, board.CreatedAt });
        }).RequireAuthorization();

        // C1: collect card image URLs before deletion and clean up files
        app.MapDelete("/api/boards/{boardId}", async (
            int boardId, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user,
            IWebHostEnvironment env, ApplicationDbContext db) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var (coverUrl, _) = await boardService.GetCoverAsync(boardId, userId!);

            var cardImageUrls = await (
                from col in db.Columns
                join card in db.Cards on col.Id equals card.ColumnId
                join img in db.CardImages on card.Id equals img.CardId
                where col.BoardId == boardId
                select img.Url
            ).ToListAsync();

            var deleted = await boardService.DeleteAsync(boardId, userId!);
            if (deleted)
            {
                CoverImageHelper.DeleteLocalImage(env, coverUrl);
                foreach (var url in cardImageUrls)
                    CoverImageHelper.DeleteLocalImage(env, url);
            }

            return deleted ? Results.NoContent() : Results.NotFound();
        }).RequireAuthorization();

        app.MapPost("/api/boards/{boardId}/cover", async (
            int boardId, IFormFile file, [FromQuery] string? position,
            IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user, IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var validationError = CoverImageHelper.ValidateImage(file);
            if (validationError != null) return Results.BadRequest(validationError);

            if (!string.IsNullOrWhiteSpace(position) && !CoverImageHelper.IsValidObjectPosition(position))
                return Results.BadRequest("Invalid position format.");

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var (oldUrl, _) = await boardService.GetCoverAsync(boardId, userId!);
            var newUrl = await CoverImageHelper.SaveImageAsync(env, "board-covers", file);
            var objectPosition = string.IsNullOrWhiteSpace(position) ? "50% 50%" : position;

            var updated = await boardService.UpdateCoverAsync(boardId, userId!, newUrl, objectPosition);
            if (updated == null)
            {
                CoverImageHelper.DeleteLocalImage(env, newUrl);
                return Results.NotFound();
            }

            CoverImageHelper.DeleteLocalImage(env, oldUrl);
            return Results.Ok(new { coverImageUrl = newUrl, coverObjectPosition = objectPosition });
        }).DisableAntiforgery().RequireAuthorization();

        app.MapDelete("/api/boards/{boardId}/cover", async (
            int boardId, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user, IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
            if (!authResult.Succeeded) return Results.Forbid();

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
            var (oldUrl, _) = await boardService.GetCoverAsync(boardId, userId!);
            var updated = await boardService.UpdateCoverAsync(boardId, userId!, null, null);
            if (updated == null) return Results.NotFound();

            CoverImageHelper.DeleteLocalImage(env, oldUrl);
            return Results.NoContent();
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
