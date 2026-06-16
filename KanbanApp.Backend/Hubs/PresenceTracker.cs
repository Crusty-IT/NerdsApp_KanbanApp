namespace KanbanApp.Backend.Hubs;

public class PresenceTracker
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, Dictionary<string, (string UserId, string UserName)>> _boards = new();

    public async Task UserJoined(string boardId, string connectionId, string userId, string userName)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_boards.TryGetValue(boardId, out var connections))
            {
                connections = new Dictionary<string, (string, string)>();
                _boards[boardId] = connections;
            }
            connections[connectionId] = (userId, userName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task UserLeft(string boardId, string connectionId)
    {
        await _lock.WaitAsync();
        try
        {
            if (_boards.TryGetValue(boardId, out var connections))
            {
                connections.Remove(connectionId);
                if (connections.Count == 0)
                    _boards.Remove(boardId);
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<string>> GetBoardsForConnection(string connectionId)
    {
        await _lock.WaitAsync();
        try
        {
            return _boards
                .Where(b => b.Value.ContainsKey(connectionId))
                .Select(b => b.Key)
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<(string UserId, string UserName)>> GetUsersOnBoard(string boardId)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_boards.TryGetValue(boardId, out var connections))
                return [];
            return connections.Values.ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
}
