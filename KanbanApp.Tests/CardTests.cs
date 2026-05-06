namespace KanbanApp.Tests;

public class CardTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public CardTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateCard_AsMember_ReturnsCreated()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_create1@test.com");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "My Task", columnId = colId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("My Task", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateCard_AsNonMember_ReturnsForbidden()
    {
        var (owner, boardId, colId) = await CreateClientWithBoardAndColumn("card_owner2@test.com");
        var outsider = await CreateAuthenticatedClient("card_outsider2@test.com");

        var response = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "Hack", columnId = colId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateCard_AsMember_ReturnsOk()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_update3@test.com");
        var cardId = await CreateCard(client, boardId, colId, "Old Title");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "New Title", columnId = colId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Title", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateCard_AsNonMember_ReturnsForbidden()
    {
        var (owner, boardId, colId) = await CreateClientWithBoardAndColumn("card_owner4@test.com");
        var cardId = await CreateCard(owner, boardId, colId, "Task");
        var outsider = await CreateAuthenticatedClient("card_outsider4@test.com");

        var response = await outsider.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Hacked", columnId = colId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_AsMember_ReturnsNoContent()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_delete5@test.com");
        var cardId = await CreateCard(client, boardId, colId, "To Delete");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_AsNonMember_ReturnsForbidden()
    {
        var (owner, boardId, colId) = await CreateClientWithBoardAndColumn("card_owner6@test.com");
        var cardId = await CreateCard(owner, boardId, colId, "Protected");
        var outsider = await CreateAuthenticatedClient("card_outsider6@test.com");

        var response = await outsider.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task MoveCard_ToDifferentColumn_ReturnsUpdatedColumnId()
    {
        var (client, boardId, col1Id) = await CreateClientWithBoardAndColumn("card_move7@test.com");
        var col2Response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "Done" });
        var col2Id = (await col2Response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var cardId = await CreateCard(client, boardId, col1Id, "Movable Task");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Movable Task", columnId = col2Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(col2Id, data.GetProperty("columnId").GetInt32());
    }

    [Fact]
    public async Task CreateCard_WithDueDate_ReturnsCorrectData()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_meta8@test.com");
        var dueDate = DateTime.UtcNow.AddDays(7).ToString("o");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards", new
        {
            title = "Meta Task",
            columnId = colId,
            dueDate
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Meta Task", data.GetProperty("title").GetString());
        Assert.True(data.TryGetProperty("dueDate", out _));
    }

    [Fact]
    public async Task AssignCard_ToValidMember_ReturnsOk()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_assign9@test.com");
        var cardId = await CreateCard(client, boardId, colId, "Assign Task");

        var me = await client.GetAsync("/api/users/me");
        var userId = (await me.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}/assign",
            new { userId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(userId, data.GetProperty("assignedToUserId").GetString());
    }

    [Fact]
    public async Task AssignCard_ToNonMember_ReturnsBadRequest()
    {
        var (client, boardId, colId) = await CreateClientWithBoardAndColumn("card_assign10@test.com");
        var cardId = await CreateCard(client, boardId, colId, "Assign Task 2");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}/assign",
            new { userId = "nonexistent-user-id" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    private async Task<HttpClient> CreateAuthenticatedClient(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email, password = "Test123!" });
        var login = await client.PostAsJsonAsync(
            "/login?useCookies=false&useSessionCookies=false",
            new { email, password = "Test123!" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString()!;
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    private async Task<(HttpClient client, int boardId, int colId)> CreateClientWithBoardAndColumn(string email)
    {
        var client = await CreateAuthenticatedClient(email);
        var board = await client.PostAsJsonAsync("/api/boards", new { boardName = "Test Board" });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var col = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "To Do" });
        var colId = (await col.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (client, boardId, colId);
    }

    private async Task<int> CreateCard(HttpClient client, int boardId, int colId, string title)
    {
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title, columnId = colId });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}