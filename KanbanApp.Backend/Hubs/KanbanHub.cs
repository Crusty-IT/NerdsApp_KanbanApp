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
        await SendEditingSnapshot(boardId);
    }

    public async Task LeaveBoard(string boardId)
    {
        var stoppedEditing = await presenceTracker.StopEditingForConnection(boardId, Context.ConnectionId);
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"board-{boardId}");
        foreach (var editing in stoppedEditing)
            await SendEditingStopped(editing);

        await presenceTracker.UserLeft(boardId, Context.ConnectionId);
        await SendPresenceUpdate(boardId);
    }

    public async Task StartEditingCard(string boardId, int cardId)
    {
        if (!int.TryParse(boardId, out var id)) return;
        if (!await IsBoardMemberAsync(id)) return;
        if (!await IsCardOnBoardAsync(id, cardId)) return;

        await presenceTracker.TrackEditing(boardId, Context.ConnectionId, cardId, UserId, UserName);
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

        var stoppedEditing = await presenceTracker.StopEditing(boardId, Context.ConnectionId, cardId);
        if (stoppedEditing != null)
            await SendEditingStopped(stoppedEditing);
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
            var stoppedEditing = await presenceTracker.StopEditingForConnection(boardId, Context.ConnectionId);
            foreach (var editing in stoppedEditing)
                await SendEditingStopped(editing);

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

    private async Task SendEditingSnapshot(string boardId)
    {
        var editingUsers = await presenceTracker.GetEditingOnBoard(boardId);
        foreach (var editing in editingUsers.Where(e => e.UserId != UserId))
        {
            await Clients.Caller.SendAsync("CardEditingStarted", new
            {
                cardId = editing.CardId,
                userId = editing.UserId,
                userName = editing.UserName
            });
        }
    }

    private async Task SendEditingStopped(EditingUser editing)
    {
        await Clients.Group($"board-{editing.BoardId}").SendAsync("CardEditingStopped", new
        {
            cardId = editing.CardId,
            userId = editing.UserId
        });
    }

    private async Task<bool> IsBoardMemberAsync(int boardId)
    {
        return await db.BoardMembers.AnyAsync(m => m.BoardId == boardId && m.UserId == UserId);
    }

    private async Task<bool> IsCardOnBoardAsync(int boardId, int cardId)
    {
        return await db.Cards.AnyAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
    }

    private string UserId => Context.User?.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
    private string UserName => Context.User?.FindFirstValue(ClaimTypes.Name) ?? string.Empty;
}
