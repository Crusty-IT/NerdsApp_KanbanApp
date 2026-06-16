namespace KanbanApp.Backend.Services;

using Models;

public interface ICardService
{
    Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description, DateTime? dueDate, int? priority);
    Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId, string? assignedToUserId, DateTime? dueDate, int? priority, string userId);
    Task<bool> DeleteAsync(int boardId, int cardId);
    Task<Card?> AssignCardAsync(int boardId, int cardId, string userId, string assignedByUserId);
    Task<List<Card>> SearchAsync(int boardId, string query);
    Task<List<CardImage>?> GetImagesAsync(int boardId, int cardId);
    Task<CardImage?> AddImageAsync(int boardId, int cardId, string fileName, string url, string contentType, string userId, string? objectPosition);
    Task<CardImage?> DeleteImageAsync(int boardId, int cardId, int imageId);
}
