using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using KanbanApp.Backend.DTOs;
using KanbanApp.Backend.Models;
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

            var card = await cardService.CreateAsync(boardId, dto.ColumnId, dto.Title, dto.Description, dto.DueDate, dto.Priority);
            return card is null
                ? Results.BadRequest("Column not found.")
                : TypedResults.Created($"/api/boards/{boardId}/cards/{card.Id}",
                    new { card.Id, card.Title, card.Description, card.ColumnId, card.DueDate, card.Priority, images = Array.Empty<CardImageDto>() });
        });

        cards.MapPut("/{cardId}", async (int boardId, int cardId, UpdateCardDto dto, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var card = await cardService.UpdateAsync(boardId, cardId, dto.Title, dto.Description, dto.ColumnId, dto.AssignedToUserId, dto.DueDate, dto.Priority);
            return card is null
                ? Results.NotFound()
                : Results.Ok(new
                {
                    card.Id,
                    card.Title,
                    card.Description,
                    card.ColumnId,
                    card.AssignedToUserId,
                    card.DueDate,
                    card.Priority,
                    Images = card.Images.Select(ToDto).ToList()
                });
        });

        cards.MapDelete("/{cardId}", async (int boardId, int cardId, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user, IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var images = await cardService.GetImagesAsync(boardId, cardId);
            var deleted = await cardService.DeleteAsync(boardId, cardId);
            if (deleted && images != null)
            {
                foreach (var image in images)
                    DeleteLocalImage(env, image.Url);
            }

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

            var assignedByUserId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var card = await cardService.AssignCardAsync(cardId, dto.UserId, assignedByUserId);
            return card is null ? Results.NotFound() : Results.Ok(new { card.Id, card.Title, card.AssignedToUserId });
        });

        cards.MapGet("/search", async (int boardId, string q, ICardService cardService,
            IAuthorizationService authorizationService, ClaimsPrincipal user) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            if (string.IsNullOrWhiteSpace(q))
                return Results.BadRequest("Query cannot be empty.");

            var results = await cardService.SearchAsync(boardId, q);
            return Results.Ok(results.Select(c => new
            {
                c.Id,
                c.Title,
                c.Description,
                c.ColumnId,
                c.AssignedToUserId,
                c.DueDate,
                c.Priority,
                Images = c.Images.Select(ToDto).ToList()
            }));
        });

        cards.MapPost("/{cardId}/images", async (int boardId, int cardId,
            IFormFile file, [FromQuery] string? position,
            ICardService cardService, IAuthorizationService authorizationService, ClaimsPrincipal user,
            IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var validationError = CoverImageHelper.ValidateImage(file);
            if (validationError != null) return Results.BadRequest(validationError);

            var uploadsFolder = Path.Combine(GetWebRoot(env), "card-images");
            Directory.CreateDirectory(uploadsFolder);

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            var fileName = $"{Guid.NewGuid():N}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);
            await using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            var userId = user.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var objectPosition = string.IsNullOrWhiteSpace(position) ? "50% 50%" : position;
            var image = await cardService.AddImageAsync(boardId, cardId, fileName, $"/card-images/{fileName}", file.ContentType, userId, objectPosition);
            if (image == null)
            {
                File.Delete(filePath);
                return Results.NotFound();
            }

            return Results.Created($"/api/boards/{boardId}/cards/{cardId}/images/{image.Id}", ToDto(image));
        }).DisableAntiforgery();

        cards.MapDelete("/{cardId}/images/{imageId}", async (int boardId, int cardId, int imageId,
            ICardService cardService, IAuthorizationService authorizationService, ClaimsPrincipal user,
            IWebHostEnvironment env) =>
        {
            var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
            if (!authResult.Succeeded) return Results.Forbid();

            var image = await cardService.DeleteImageAsync(boardId, cardId, imageId);
            if (image == null) return Results.NotFound();

            DeleteLocalImage(env, image.Url);
            return Results.NoContent();
        });
    }

    private static CardImageDto ToDto(CardImage image)
        => new(image.Id, image.FileName, image.Url, image.ContentType, image.ObjectPosition, image.UploadedAt);

    private static string GetWebRoot(IWebHostEnvironment env)
        => env.WebRootPath ?? Path.Combine(env.ContentRootPath, "wwwroot");

    private static void DeleteLocalImage(IWebHostEnvironment env, string url)
    {
        if (string.IsNullOrEmpty(url) || url.StartsWith("http")) return;
        var filePath = Path.Combine(GetWebRoot(env), url.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
        if (File.Exists(filePath)) File.Delete(filePath);
    }
}
