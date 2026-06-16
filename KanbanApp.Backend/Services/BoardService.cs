namespace KanbanApp.Backend.Services;

using Data;
using Models;
using Microsoft.EntityFrameworkCore;

public class BoardService : IBoardService
{
    private readonly ApplicationDbContext _context;

    public BoardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Board>> GetAllByUserAsync(string userId)
    {
        return await _context.Boards
            .Where(b => b.BoardMembers.Any(bm => bm.UserId == userId))
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public async Task<Board?> GetByIdAsync(int boardId, string userId)
    {
        return await _context.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .ThenInclude(c => c.Images)
            .FirstOrDefaultAsync(b => b.Id == boardId
                                      && b.BoardMembers.Any(bm => bm.UserId == userId));
    }

    public async Task<Board> CreateAsync(string name, string? description, string ownerUserId, int? projectId = null, string? color = null)
    {
        var board = new Board
        {
            Name = name,
            Description = description,
            ProjectId = projectId,
            Color = color ?? "#00d4ff"
        };

        _context.Boards.Add(board);
        _context.BoardMembers.Add(new BoardMember { Board = board, UserId = ownerUserId, Role = BoardRole.Owner });

        await _context.SaveChangesAsync();
        return board;
    }

    public async Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description, string? color = null)
    {
        var board = await GetByIdAsync(boardId, userId);
        if (board == null) return null;

        board.Name = name;
        board.Description = description;
        if (color != null) board.Color = color;

        await _context.SaveChangesAsync();
        return board;
    }

    public async Task<bool> DeleteAsync(int boardId, string userId)
    {
        var board = await _context.Boards
            .Include(b => b.Columns)
            .ThenInclude(c => c.Cards)
            .ThenInclude(c => c.Images)
            .Include(b => b.BoardMembers)
            .FirstOrDefaultAsync(b => b.Id == boardId
                                      && b.BoardMembers.Any(bm => bm.UserId == userId));

        if (board == null) return false;

        _context.Cards.RemoveRange(board.Columns.SelectMany(c => c.Cards));
        _context.Columns.RemoveRange(board.Columns);
        _context.BoardMembers.RemoveRange(board.BoardMembers);
        _context.Boards.Remove(board);

        await _context.SaveChangesAsync();
        return true;
    }

    public async Task<bool> IsMemberAsync(int boardId, string userId)
    {
        return await _context.BoardMembers
            .AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
    }

    public async Task<Board?> UpdateCoverAsync(int boardId, string userId, string? coverImageUrl, string? coverObjectPosition)
    {
        var board = await _context.Boards
            .FirstOrDefaultAsync(b => b.Id == boardId && b.BoardMembers.Any(bm => bm.UserId == userId));
        if (board == null) return null;

        board.CoverImageUrl = coverImageUrl;
        board.CoverObjectPosition = coverObjectPosition;
        await _context.SaveChangesAsync();
        return board;
    }

    public async Task<(string? Url, string? Position)> GetCoverAsync(int boardId, string userId)
    {
        var board = await _context.Boards
            .Where(b => b.Id == boardId && b.BoardMembers.Any(bm => bm.UserId == userId))
            .Select(b => new { b.CoverImageUrl, b.CoverObjectPosition })
            .FirstOrDefaultAsync();
        return board == null ? (null, null) : (board.CoverImageUrl, board.CoverObjectPosition);
    }
}
