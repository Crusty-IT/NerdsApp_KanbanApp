namespace KanbanApp.Backend.Services;

using Models;

public interface IBoardService
{
    Task<IEnumerable<Board>> GetAllByUserAsync(string userId);
    Task<Board?> GetByIdAsync(int boardId, string userId);
    Task<Board> CreateAsync(string name, string? description, string ownerUserId, int? projectId = null, string? color = null);
    Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description, string? color = null);
    Task<bool> DeleteAsync(int boardId, string userId);
    Task<bool> IsMemberAsync(int boardId, string userId);
    Task<Board?> UpdateCoverAsync(int boardId, string userId, string? coverImageUrl, string? coverObjectPosition);
    Task<(string? Url, string? Position)> GetCoverAsync(int boardId, string userId);
}
