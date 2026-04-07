namespace KanbanApp.Backend.Services;

using Data;
using Models;
using Microsoft.EntityFrameworkCore;

public class CardService : ICardService
{
    private readonly ApplicationDbContext _context;

    public CardService(ApplicationDbContext context) { _context = context; }

    public async Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description, DateTime? dueDate, string? color)
    {
        var column = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (column == null) return null;

        var card = new Card
        {
            Title = title,
            Description = description,
            ColumnId = columnId,
            DueDate = dueDate,
            Color = color
        };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return card;
    }

    public async Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, string? color)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return null;

        var targetColumn = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (targetColumn == null) return null;

        card.Title = title;
        card.Description = description;
        card.ColumnId = columnId;
        card.AssignedToUserId = assignedToUserId;
        card.DueDate = dueDate;
        card.Color = color;
        await _context.SaveChangesAsync();
        return card;
    }

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

        // - tworzymy powiadomienie tylko jeśli przypisujemy nowego użytkownika
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
            .Where(c => c.Column.BoardId == boardId &&
                        (c.Title.ToLower().Contains(q) ||
                         (c.Description != null && c.Description.ToLower().Contains(q))))
            .ToListAsync();
    }
}