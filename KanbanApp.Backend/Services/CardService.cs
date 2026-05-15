namespace KanbanApp.Backend.Services;

using Data;
using Models;
using Microsoft.EntityFrameworkCore;

public class CardService : ICardService
{
    private readonly ApplicationDbContext _context;

    public CardService(ApplicationDbContext context) { _context = context; }

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
        return card;
    }

    public async Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, int? priority)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .Include(c => c.Images)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return null;

        var targetColumn = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (targetColumn == null) return null;

        card.Title = title;
        card.Description = description;
        card.ColumnId = columnId;
        card.AssignedToUserId = assignedToUserId;
        card.DueDate = ToUtc(dueDate);
        card.Priority = priority;
        await _context.SaveChangesAsync();
        return card;
    }

    private static DateTime? ToUtc(DateTime? value)
        => value.HasValue && value.Value.Kind == DateTimeKind.Unspecified
            ? DateTime.SpecifyKind(value.Value, DateTimeKind.Utc)
            : value?.ToUniversalTime();

    public async Task<bool> DeleteAsync(int boardId, int cardId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return false;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<Card?> AssignCardAsync(int cardId, string userId, string assignedByUserId)
    {
        var card = await _context.Cards.FindAsync(cardId);
        if (card == null) return null;

        var previousUserId = card.AssignedToUserId;
        card.AssignedToUserId = userId;

        if (!string.IsNullOrEmpty(userId) && userId != previousUserId)
        {
            var assignedByUser = await _context.Users.FindAsync(assignedByUserId);
            var notification = new Notification
            {
                UserId = userId,
                CardId = cardId,
                Message = $"You have been assigned to card \"{card.Title}\" by {assignedByUser?.UserName ?? "someone"}."
            };
            _context.Notifications.Add(notification);
        }

        await _context.SaveChangesAsync();
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
}
