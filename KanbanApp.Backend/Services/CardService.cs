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
    private readonly IPushNotificationService _pushNotifications;

    public CardService(ApplicationDbContext context, IHubContext<KanbanHub> hub, IPushNotificationService pushNotifications)
    {
        _context = context;
        _hub = hub;
        _pushNotifications = pushNotifications;
    }

    public async Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description, DateTime? dueDate, int? priority, string userId)
    {
        var column = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (column == null) return null;

        var lastPosition = await _context.Cards
            .Where(c => c.ColumnId == columnId)
            .Select(c => (int?)c.Position)
            .MaxAsync();
        var position = (lastPosition ?? -1) + 1;
        var card = new Card
        {
            Title = title,
            Description = description,
            ColumnId = columnId,
            Position = position,
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

        await NotifyBoardMembersAsync(boardId, userId, $"Card \"{card.Title}\" was created.", card.Id, NotificationEventTypes.CardCreated);

        return card;
    }

    public async Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, int? priority, int? position, string userId)
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
        var fromPosition = card.Position;
        var previousAssignedToUserId = card.AssignedToUserId;

        card.Title = title;
        card.Description = description;
        card.AssignedToUserId = assignedToUserId;
        card.DueDate = ToUtc(dueDate);
        card.Priority = priority;

        await RepositionCardAsync(boardId, card, fromColumnId, columnId, position);
        await _context.SaveChangesAsync();

        var group = _hub.Clients.Group($"board-{boardId}");

        var moved = fromColumnId != card.ColumnId || fromPosition != card.Position;
        if (moved)
        {
            await group.SendAsync("CardMoved", new
            {
                cardId,
                fromColumnId,
                toColumnId = card.ColumnId,
                newPosition = card.Position,
                movedByUserId = userId
            });
        }

        await group.SendAsync("CardUpdated", new { card = ToCardDto(card) });

        if (!string.IsNullOrEmpty(assignedToUserId) && assignedToUserId != previousAssignedToUserId)
        {
            var assignedByUser = await _context.Users.FindAsync(userId);
            await NotifyUserAsync(
                assignedToUserId,
                $"You have been assigned to card \"{card.Title}\" by {assignedByUser?.UserName ?? "someone"}.",
                card.Id,
                NotificationEventTypes.CardAssigned);
        }

        var extraExcludedUserIds = !string.IsNullOrEmpty(assignedToUserId) && assignedToUserId != previousAssignedToUserId
            ? new[] { assignedToUserId }
            : Array.Empty<string>();

        await NotifyBoardMembersAsync(
            boardId,
            userId,
            moved ? $"Card \"{card.Title}\" was moved." : $"Card \"{card.Title}\" was updated.",
            card.Id,
            moved ? NotificationEventTypes.CardMoved : NotificationEventTypes.CardUpdated,
            extraExcludedUserIds);

        return card;
    }

    public async Task<bool> DeleteAsync(int boardId, int cardId, string userId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return false;

        var columnId = card.ColumnId;
        var title = card.Title;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        await NormalizeColumnAsync(columnId);
        await _context.SaveChangesAsync();

        await _hub.Clients.Group($"board-{boardId}").SendAsync("CardDeleted", new { cardId, columnId });
        await NotifyBoardMembersAsync(boardId, userId, $"Card \"{title}\" was deleted.", cardId, NotificationEventTypes.CardDeleted);

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
            await SendNotificationAsync(notification, NotificationEventTypes.CardAssigned);
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

    private async Task RepositionCardAsync(int boardId, Card card, int fromColumnId, int toColumnId, int? requestedPosition)
    {
        var sourceCards = await _context.Cards
            .Include(c => c.Column)
            .Where(c => c.Column.BoardId == boardId && c.ColumnId == fromColumnId && c.Id != card.Id)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Id)
            .ToListAsync();

        var targetCards = fromColumnId == toColumnId
            ? sourceCards
            : await _context.Cards
                .Include(c => c.Column)
                .Where(c => c.Column.BoardId == boardId && c.ColumnId == toColumnId && c.Id != card.Id)
                .OrderBy(c => c.Position)
                .ThenBy(c => c.Id)
                .ToListAsync();

        if (fromColumnId != toColumnId)
        {
            for (var i = 0; i < sourceCards.Count; i++)
                sourceCards[i].Position = i;
        }

        var fallbackPosition = fromColumnId == toColumnId ? card.Position : targetCards.Count;
        var insertAt = Math.Clamp(requestedPosition ?? fallbackPosition, 0, targetCards.Count);

        card.ColumnId = toColumnId;
        targetCards.Insert(insertAt, card);
        for (var i = 0; i < targetCards.Count; i++)
        {
            targetCards[i].ColumnId = toColumnId;
            targetCards[i].Position = i;
        }
    }

    private async Task NormalizeColumnAsync(int columnId)
    {
        var cards = await _context.Cards
            .Where(c => c.ColumnId == columnId)
            .OrderBy(c => c.Position)
            .ThenBy(c => c.Id)
            .ToListAsync();

        for (var i = 0; i < cards.Count; i++)
            cards[i].Position = i;
    }

    private async Task NotifyBoardMembersAsync(
        int boardId,
        string actorUserId,
        string message,
        int cardId,
        string eventType,
        IReadOnlyCollection<string>? extraExcludedUserIds = null)
    {
        var excludedUserIds = new HashSet<string>(extraExcludedUserIds ?? Array.Empty<string>())
        {
            actorUserId
        };

        var memberIds = await _context.BoardMembers
            .Where(m => m.BoardId == boardId && !excludedUserIds.Contains(m.UserId))
            .Select(m => m.UserId)
            .ToListAsync();

        if (memberIds.Count == 0) return;

        var notifications = memberIds.Select(userId => new Notification
        {
            UserId = userId,
            CardId = cardId,
            Message = message
        }).ToList();

        _context.Notifications.AddRange(notifications);
        await _context.SaveChangesAsync();

        foreach (var notification in notifications)
            await SendNotificationAsync(notification, eventType);
    }

    private async Task NotifyUserAsync(string userId, string message, int cardId, string eventType)
    {
        var notification = new Notification
        {
            UserId = userId,
            CardId = cardId,
            Message = message
        };

        _context.Notifications.Add(notification);
        await _context.SaveChangesAsync();
        await SendNotificationAsync(notification, eventType);
    }

    private async Task SendNotificationAsync(Notification notification, string eventType)
    {
        await _hub.Clients.User(notification.UserId).SendAsync("NotificationReceived", new
        {
            id = notification.Id,
            message = notification.Message,
            isRead = false,
            createdAt = notification.CreatedAt,
            cardId = notification.CardId
        });

        await _pushNotifications.SendToUserAsync(notification.UserId, notification.Message, notification.CardId, eventType);
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
