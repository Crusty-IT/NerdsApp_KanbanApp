namespace KanbanApp.Backend.Services;

using Models;

public interface ICardService
{
    Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description, DateTime? dueDate, string? color);
    Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, string? color);
    Task<bool> DeleteAsync(int boardId, int cardId);
    Task<Card?> AssignCardAsync(int cardId, string userId, string assignedByUserId);
    Task<List<Card>> SearchAsync(int boardId, string query);
}