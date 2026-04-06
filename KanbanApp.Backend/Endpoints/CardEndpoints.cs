using System.Security.Claims;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace KanbanApp.Backend.Endpoints;

public static class CardEndpoints
{
    public static void MapCardEndpoints(this WebApplication app)
    {
        var cards = app.MapGroup("/api/boards/{boardId}/cards")
            .RequireAuthorization();

        cards.MapPost("/", async (int boardId, CreateCardDto dto, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var card = await cardService.CreateAsync(boardId, dto.ColumnId, dto.Title, dto.Description, dto.DueDate, dto.Color);
            return card is null
                ? Results.BadRequest("Column not found.")
                : TypedResults.Created($"/api/boards/{boardId}/cards/{card.Id}",
                    new { card.Id, card.Title, card.Description, card.ColumnId, card.DueDate, card.Color });
        });

        cards.MapPut("/{cardId}", async (int boardId, int cardId, UpdateCardDto dto, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var card = await cardService.UpdateAsync(boardId, cardId, dto.Title, dto.Description, dto.ColumnId, dto.AssignedToUserId, dto.DueDate, dto.Color);
            return card is null
                ? Results.NotFound()
                : Results.Ok(new { card.Id, card.Title, card.Description, card.ColumnId, card.AssignedToUserId, card.DueDate, card.Color });
        });

        cards.MapDelete("/{cardId}", async (int boardId, int cardId, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var deleted = await cardService.DeleteAsync(boardId, cardId);
            return deleted ? Results.NoContent() : Results.NotFound();
        });

        cards.MapPut("/{cardId}/assign", async (int boardId, int cardId, AssignCardDto dto,
            ICardService cardService, IBoardService boardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var isMember = await boardService.IsMemberAsync(boardId, dto.UserId);
            if (!isMember) return Results.BadRequest("User is not a board member.");

            var card = await cardService.AssignCardAsync(cardId, dto.UserId);
            return card is null ? Results.NotFound() : Results.Ok(new { card.Id, card.Title, card.AssignedToUserId });
        });
    }
}