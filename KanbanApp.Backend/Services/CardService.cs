namespace KanbanApp.Backend.Services;

using Data;
using DTOs;
using Models;
using Hubs;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

public class CardService : ICardService
{
    private readonly ApplicationDbContext _context;
    private readonly IHubContext<KanbanHub> _hub;

    public CardService(ApplicationDbContext context, IHubContext<KanbanHub> hub)
    {
        _context = context;
        _hub = hub;
    }

    public async Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description, DateTime? dueDate, int? priority)
    {
        var column = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (column == null) return null;

        var card = new Card
        {
            Title = title,
            Description = description,
            ColumnId = columnId,
            DueDate = ToUtc(dueDate),
            Priority = priority
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();

        await _hub.Clients.Group($"board-{boardId}").SendAsync("CardCreated", new
        {
            card = ToCardDto(card),
            columnId
        });

        return card;
    }

    public async Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, int? priority, string userId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return null;

        var targetColumn = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (targetColumn == null) return null;

        var fromColumnId = card.ColumnId;

        card.Title = title;
        card.Description = description;
        card.ColumnId = columnId;
        card.AssignedToUserId = assignedToUserId;
        card.DueDate = ToUtc(dueDate);
        card.Priority = priority;
        await _context.SaveChangesAsync();

        var group = _hub.Clients.Group($"board-{boardId}");

        if (fromColumnId != columnId)
        {
            await group.SendAsync("CardMoved", new
            {
                cardId,
                fromColumnId,
                toColumnId = columnId,
                newPosition = card.Position,
                movedByUserId = userId
            });
        }

        await group.SendAsync("CardUpdated", new { card = ToCardDto(card) });

        return card;
    }

    public async Task<bool> DeleteAsync(int boardId, int cardId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return false;

        var columnId = card.ColumnId;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();

        await _hub.Clients.Group($"board-{boardId}").SendAsync("CardDeleted", new { cardId, columnId });

        return true;
    }

    public async Task<Card?> AssignCardAsync(int boardId, int cardId, string userId, string assignedByUserId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return null;

        var previousUserId = card.AssignedToUserId;
        card.AssignedToUserId = userId;

        Notification? notification = null;
        if (!string.IsNullOrEmpty(userId) && userId != previousUserId)
        {
            var assignedByUser = await _context.Users.FindAsync(assignedByUserId);
            notification = new Notification
            {
                UserId = userId,
                CardId = cardId,
                Message = $"You have been assigned to card \"{card.Title}\" by {assignedByUser?.UserName ?? "someone"}."
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();

        if (notification != null)
        {
            await _hub.Clients.User(userId).SendAsync("NotificationReceived", new
            {
                id = notification.Id,
                message = notification.Message,
                isRead = false,
                createdAt = notification.CreatedAt,
                cardId = notification.CardId
            });
        }

        var assignedUser = string.IsNullOrEmpty(userId) ? null : await _context.Users.FindAsync(userId);
        await _hub.Clients.Group($"board-{boardId}").SendAsync("CardAssigned", new
        {
            cardId,
            assignedToUserId = userId,
            assignedToUserName = assignedUser?.UserName
        });

        return card;
    }

    public async Task<List<Card>> SearchAsync(int boardId, string query)
    {
        var q = query.ToLower();
        return await _context.Cards
            .Include(c => c.Column)
            .Include(c => c.Images)
            .Where(c => c.Column.BoardId == boardId &&
                        (c.Title.ToLower().Contains(q) ||
                         (c.Description != null && c.Description.ToLower().Contains(q))))
            .Take(100)
            .ToListAsync();
    }

    public async Task<List<CardImage>?> GetImagesAsync(int boardId, int cardId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        return card?.Images.ToList();
    }

    public async Task<CardImage?> AddImageAsync(int boardId, int cardId, string fileName, string url, string contentType, string userId, string? objectPosition)
    {
        var cardExists = await _context.Cards
            .Include(c => c.Column)
            .AnyAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (!cardExists) return null;

        var image = new CardImage
        {
            CardId = cardId,
            FileName = fileName,
            Url = url,
            ContentType = contentType,
            ObjectPosition = objectPosition,
            UploadedByUserId = userId
        };

        _context.CardImages.Add(image);
        await _context.SaveChangesAsync();
        return image;
    }

    public async Task<CardImage?> DeleteImageAsync(int boardId, int cardId, int imageId)
    {
        var image = await _context.CardImages
            .Include(i => i.Card)
            .ThenInclude(c => c.Column)
            .FirstOrDefaultAsync(i => i.Id == imageId && i.CardId == cardId && i.Card.Column.BoardId == boardId);
        if (image == null) return null;

        _context.CardImages.Remove(image);
        await _context.SaveChangesAsync();
        return image;
    }

    private static DateTime? ToUtc(DateTime? value)
        => value.HasValue && value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value?.ToUniversalTime();

    private static CardDto ToCardDto(Card card) => new(
        card.Id, card.Title, card.Description, card.Position, card.ColumnId,
        card.CreatedAt, card.AssignedToUserId, card.DueDate, card.Priority,
        card.Images.Select(i => new CardImageDto(i.Id, i.FileName, i.Url, i.ContentType, i.ObjectPosition, i.UploadedAt)).ToList()
    );
}
