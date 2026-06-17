using System.Security.Claims;
using KanbanApp.Backend.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;

namespace KanbanApp.Backend.Hubs;

[Authorize]
public class KanbanHub(PresenceTracker presenceTracker, ILogger<KanbanHub> logger, ApplicationDbContext db) : Hub
{
    // S6: only board members may join the real-time group for that board
    public async Task JoinBoard(string boardId)
    {
        if (!int.TryParse(boardId, out var id)) return;
        if (!await IsBoardMemberAsync(id)) return;

        await Groups.AddToGroupAsync(Context.ConnectionId, $"board-{boardId}");
        await presenceTracker.UserJoined(boardId, Context.ConnectionId, UserId, UserName);
        await SendPresenceUpdate(boardId);
    }

    public async Task LeaveBoard(string boardId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"board-{boardId}");
        await presenceTracker.UserLeft(boardId, Context.ConnectionId);
        await SendPresenceUpdate(boardId);
    }

    public async Task StartEditingCard(string boardId, int cardId)
    {
        if (!int.TryParse(boardId, out var id)) return;
        if (!await IsBoardMemberAsync(id)) return;

        await Clients.Group($"board-{boardId}").SendAsync("CardEditingStarted", new
        {
            cardId,
            userId = UserId,
            userName = UserName
        });
    }

    public async Task StopEditingCard(string boardId, int cardId)
    {
        if (!int.TryParse(boardId, out var id)) return;
        if (!await IsBoardMemberAsync(id)) return;

        await Clients.Group($"board-{boardId}").SendAsync("CardEditingStopped", new
        {
            cardId,
            userId = UserId
        });
    }

    public override async Task OnConnectedAsync()
    {
        logger.LogInformation("User {UserId} connected: {ConnectionId}", UserId, Context.ConnectionId);
        await base.OnConnectedAsync();
    }

    public override async Task OnDisconnectedAsync(Exception? exception)
    {
        var boards = await presenceTracker.GetBoardsForConnection(Context.ConnectionId);
        foreach (var boardId in boards)
        {
            await presenceTracker.UserLeft(boardId, Context.ConnectionId);
            await SendPresenceUpdate(boardId);
        }
        await base.OnDisconnectedAsync(exception);
    }

    private async Task SendPresenceUpdate(string boardId)
    {
        var users = await presenceTracker.GetUsersOnBoard(boardId);
        await Clients.Group($"board-{boardId}").SendAsync(
            "PresenceUpdated",
            users.Select(u => new { userId = u.UserId, userName = u.UserName }));
    }

    private async Task<bool> IsBoardMemberAsync(int boardId)
    {
        return await db.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == UserId);
    }

    private string UserId => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UserName => Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
