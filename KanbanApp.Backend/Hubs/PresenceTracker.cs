namespace KanbanApp.Backend.Hubs;

public class PresenceTracker
{
    private readonly SemaphoreSlim _lock = new(1, 1);
    private readonly Dictionary<string, Dictionary<string, (string UserId, string UserName)>> _boards = new();
    private readonly Dictionary<string, Dictionary<int, Dictionary<string, (string UserId, string UserName)>>> _editing = new();

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

    public async Task TrackEditing(string boardId, string connectionId, int cardId, string userId, string userName)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_editing.TryGetValue(boardId, out var cards))
            {
                cards = new Dictionary<int, Dictionary<string, (string, string)>>();
                _editing[boardId] = cards;
            }

            if (!cards.TryGetValue(cardId, out var connections))
            {
                connections = new Dictionary<string, (string, string)>();
                cards[cardId] = connections;
            }

            connections[connectionId] = (userId, userName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<EditingUser?> StopEditing(string boardId, string connectionId, int cardId)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_editing.TryGetValue(boardId, out var cards)) return null;
            if (!cards.TryGetValue(cardId, out var connections)) return null;
            if (!connections.Remove(connectionId, out var removedUser)) return null;

            var userStillEditing = connections.Values.Any(u => u.UserId == removedUser.UserId);
            if (connections.Count == 0) cards.Remove(cardId);
            if (cards.Count == 0) _editing.Remove(boardId);

            return userStillEditing
                ? null
                : new EditingUser(boardId, cardId, removedUser.UserId, removedUser.UserName);
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<EditingUser>> StopEditingForConnection(string boardId, string connectionId)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_editing.TryGetValue(boardId, out var cards)) return [];

            var stopped = new List<EditingUser>();
            foreach (var (cardId, connections) in cards.ToList())
            {
                if (!connections.Remove(connectionId, out var removedUser)) continue;

                if (!connections.Values.Any(u => u.UserId == removedUser.UserId))
                    stopped.Add(new EditingUser(boardId, cardId, removedUser.UserId, removedUser.UserName));

                if (connections.Count == 0)
                    cards.Remove(cardId);
            }

            if (cards.Count == 0)
                _editing.Remove(boardId);

            return stopped;
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
            return connections.Values
                .GroupBy(u => u.UserId)
                .Select(g => g.First())
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }

    public async Task<IReadOnlyList<EditingUser>> GetEditingOnBoard(string boardId)
    {
        await _lock.WaitAsync();
        try
        {
            if (!_editing.TryGetValue(boardId, out var cards)) return [];

            return cards
                .SelectMany(card => card.Value.Values
                    .GroupBy(u => u.UserId)
                    .Select(g => new EditingUser(boardId, card.Key, g.Key, g.First().UserName)))
                .ToList();
        }
        finally
        {
            _lock.Release();
        }
    }
}

public record EditingUser(string BoardId, int CardId, string UserId, string UserName);
