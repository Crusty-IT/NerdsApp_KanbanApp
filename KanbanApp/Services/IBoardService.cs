namespace KanbanApp.Services;

using Models;

public interface IBoardService
{
    Task<IEnumerable<Board>> GetAllByUserAsync(string userId);

    Task<Board?> GetByIdAsync(int boardId, string userId);

    Task<Board> CreateAsync(string name, string? description, string ownerUserId);

    Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description);

    Task<bool> DeleteAsync(int boardId, string userId);
}