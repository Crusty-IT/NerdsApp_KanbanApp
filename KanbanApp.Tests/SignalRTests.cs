using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace KanbanApp.Tests;

public class SignalRTests : TestBase
{
    public SignalRTests(KanbanWebAppFactory factory) : base(factory) { }

    private async Task<(string Token, HttpClient Client)> CreateUserAsync(string email)
    {
        var client = Factory.CreateClient();
        const string password = "Test123!";
        await client.PostAsJsonAsync("/register", new { email, password });
        var loginRes = await client.PostAsJsonAsync("/login", new { email, password });
        loginRes.EnsureSuccessStatusCode();
        var data = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var token = data.GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return (token, client);
    }

    private HubConnection BuildConnection(string? accessToken)
    {
        return new HubConnectionBuilder()
            .WithUrl("http://localhost/hubs/kanban", options =>
            {
                options.HttpMessageHandlerFactory = _ => Factory.Server.CreateHandler();
                options.Transports = HttpTransportType.LongPolling;
                if (accessToken != null)
                    options.AccessTokenProvider = () => Task.FromResult<string?>(accessToken);
            })
            .Build();
    }

    private async Task<HubConnection> CreateSignalRConnectionAsync(string accessToken, string boardId)
    {
        var conn = BuildConnection(accessToken);
        await conn.StartAsync();
        await conn.InvokeAsync("JoinBoard", boardId);
        return conn;
    }

    [Fact]
    public async Task CanConnectToHub_WithValidToken()
    {
        var (token, _) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var conn = BuildConnection(token);
        try
        {
            await conn.StartAsync();
            conn.State.Should().Be(HubConnectionState.Connected);
        }
        finally
        {
            await conn.StopAsync();
        }
    }

    [Fact]
    public async Task CannotConnect_WithoutToken()
    {
        var conn = BuildConnection(null);
        try
        {
            var act = async () => await conn.StartAsync();
            await act.Should().ThrowAsync<HttpRequestException>();
        }
        finally
        {
            await conn.StopAsync();
        }
    }

    [Fact]
    public async Task CardMoved_BroadcastsToOtherBoardMembers()
    {
        var email2 = $"{Guid.NewGuid()}@test.com";
        var (token1, client1) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var (token2, _) = await CreateUserAsync(email2);

        var boardId = await CreateBoardAsync(client1, "Broadcast Test Board");
        await client1.PostAsJsonAsync($"/api/boards/{boardId}/members", new { email = email2 });
        var col1Id = await CreateColumnAsync(client1, boardId, "Column A");
        var col2Id = await CreateColumnAsync(client1, boardId, "Column B");
        var cardId = await CreateCardAsync(client1, boardId, col1Id, "Moveable Card");

        var conn2 = await CreateSignalRConnectionAsync(token2, boardId.ToString());
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conn2.On<JsonElement>("CardMoved", payload => tcs.TrySetResult(payload));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            await client1.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}", new
            {
                title = "Moveable Card",
                description = (string?)null,
                columnId = col2Id,
                assignedToUserId = (string?)null,
                dueDate = (DateTime?)null,
                priority = (int?)null
            });

            var result = await tcs.Task;
            result.GetProperty("cardId").GetInt32().Should().Be(cardId);
            result.GetProperty("fromColumnId").GetInt32().Should().Be(col1Id);
            result.GetProperty("toColumnId").GetInt32().Should().Be(col2Id);
        }
        finally
        {
            await conn2.StopAsync();
        }
    }

    [Fact]
    public async Task CardCreated_BroadcastsToOtherBoardMembers()
    {
        var email2 = $"{Guid.NewGuid()}@test.com";
        var (token1, client1) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var (token2, client2) = await CreateUserAsync(email2);

        var boardId = await CreateBoardAsync(client1, "CardCreated Board");
        await client1.PostAsJsonAsync($"/api/boards/{boardId}/members", new { email = email2 });
        var colId = await CreateColumnAsync(client1, boardId, "To Do");

        var conn2 = await CreateSignalRConnectionAsync(token2, boardId.ToString());
        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conn2.On<JsonElement>("CardCreated", payload => tcs.TrySetResult(payload));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            await client1.PostAsJsonAsync($"/api/boards/{boardId}/cards",
                new { title = "New Card", columnId = colId });

            var result = await tcs.Task;
            result.GetProperty("card").GetProperty("title").GetString().Should().Be("New Card");
        }
        finally
        {
            await conn2.StopAsync();
        }
    }

    [Fact]
    public async Task PresenceUpdated_SentOnJoin()
    {
        var email2 = $"{Guid.NewGuid()}@test.com";
        var (token1, client1) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var (token2, _) = await CreateUserAsync(email2);

        var boardId = await CreateBoardAsync(client1, "Presence Join Board");
        await client1.PostAsJsonAsync($"/api/boards/{boardId}/members", new { email = email2 });
        var boardIdStr = boardId.ToString();

        var conn1 = BuildConnection(token1);
        var conn2 = BuildConnection(token2);

        var tcsJoin = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        conn1.On<JsonElement>("PresenceUpdated", payload =>
        {
            if (Interlocked.Increment(ref count) == 2) tcsJoin.TrySetResult(payload);
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcsJoin.TrySetCanceled());

        try
        {
            await conn1.StartAsync();
            await conn1.InvokeAsync("JoinBoard", boardIdStr);
            await Task.Delay(100);

            await conn2.StartAsync();
            await conn2.InvokeAsync("JoinBoard", boardIdStr);

            var joinPayload = await tcsJoin.Task;
            joinPayload.GetArrayLength().Should().Be(2);
        }
        finally
        {
            await conn1.StopAsync();
            await conn2.StopAsync();
        }
    }

    [Fact]
    public async Task PresenceUpdated_SentOnLeave()
    {
        var email2 = $"{Guid.NewGuid()}@test.com";
        var (token1, client1) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var (token2, _) = await CreateUserAsync(email2);

        var boardId = await CreateBoardAsync(client1, "Presence Leave Board");
        await client1.PostAsJsonAsync($"/api/boards/{boardId}/members", new { email = email2 });
        var boardIdStr = boardId.ToString();

        var conn1 = BuildConnection(token1);
        var conn2 = BuildConnection(token2);

        var tcsLeave = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        var count = 0;
        conn1.On<JsonElement>("PresenceUpdated", payload =>
        {
            if (Interlocked.Increment(ref count) == 3) tcsLeave.TrySetResult(payload);
        });

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcsLeave.TrySetCanceled());

        try
        {
            await conn1.StartAsync();
            await conn1.InvokeAsync("JoinBoard", boardIdStr);
            await Task.Delay(100);

            await conn2.StartAsync();
            await conn2.InvokeAsync("JoinBoard", boardIdStr);
            await Task.Delay(100);

            await conn2.InvokeAsync("LeaveBoard", boardIdStr);

            var leavePayload = await tcsLeave.Task;
            leavePayload.GetArrayLength().Should().Be(1);
        }
        finally
        {
            await conn1.StopAsync();
            await conn2.StopAsync();
        }
    }

    [Fact]
    public async Task StartEditingCard_BroadcastsCardEditingStarted()
    {
        var email2 = $"{Guid.NewGuid()}@test.com";
        var (token1, client1) = await CreateUserAsync($"{Guid.NewGuid()}@test.com");
        var (token2, _) = await CreateUserAsync(email2);

        var boardId = await CreateBoardAsync(client1, "Edit Test Board");
        await client1.PostAsJsonAsync($"/api/boards/{boardId}/members", new { email = email2 });
        var boardIdStr = boardId.ToString();
        const int cardId = 99;

        var conn1 = await CreateSignalRConnectionAsync(token1, boardIdStr);
        var conn2 = await CreateSignalRConnectionAsync(token2, boardIdStr);

        var tcs = new TaskCompletionSource<JsonElement>(TaskCreationOptions.RunContinuationsAsynchronously);
        conn2.On<JsonElement>("CardEditingStarted", payload => tcs.TrySetResult(payload));

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        cts.Token.Register(() => tcs.TrySetCanceled());

        try
        {
            await conn1.InvokeAsync("StartEditingCard", boardIdStr, cardId);
            var result = await tcs.Task;
            result.GetProperty("cardId").GetInt32().Should().Be(cardId);
        }
        finally
        {
            await conn1.StopAsync();
            await conn2.StopAsync();
        }
    }
}
