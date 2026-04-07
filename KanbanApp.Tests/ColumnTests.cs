namespace KanbanApp.Tests;

public class ColumnTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public ColumnTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task CreateColumn_AsMember_ReturnsCreated()
    {
        var (client, boardId) = await CreateClientWithBoard("col_member1@test.com");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "To Do" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateColumn_AsNonMember_ReturnsForbidden()
    {
        var (owner, boardId) = await CreateClientWithBoard("col_owner2@test.com");
        var outsider = await CreateAuthenticatedClient("col_outsider2@test.com");

        var response = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "Hack" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateColumn_AsMember_ReturnsOk()
    {
        var (client, boardId) = await CreateClientWithBoard("col_member3@test.com");
        var colId = await CreateColumn(client, boardId, "To Do");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}",
            new { name = "Done" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Done", data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteColumn_Empty_ReturnsNoContent()
    {
        var (client, boardId) = await CreateClientWithBoard("col_member4@test.com");
        var colId = await CreateColumn(client, boardId, "To Delete");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_WithCards_ClearsCardsAndReturnsNoContent()
    {
        var (client, boardId) = await CreateClientWithBoard("col_member5@test.com");
        var colId = await CreateColumn(client, boardId, "With Cards");

        await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "Card 1", columnId = colId });
        await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "Card 2", columnId = colId });

        var clearResponse = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}/cards");
        Assert.Equal(HttpStatusCode.NoContent, clearResponse.StatusCode);

        var deleteResponse = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}");
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);
    }

    [Fact]
    public async Task UpdateColumn_WithColor_ReturnsUpdatedColor()
    {
        var (client, boardId) = await CreateClientWithBoard("col_member6@test.com");
        var colId = await CreateColumn(client, boardId, "Colored");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}",
            new { name = "Colored", color = "#ef4444" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#ef4444", data.GetProperty("color").GetString());
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

    private async Task<(HttpClient client, int boardId)> CreateClientWithBoard(string email)
    {
        var client = await CreateAuthenticatedClient(email);
        var board = await client.PostAsJsonAsync("/api/boards", new { boardName = "Test Board" });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (client, boardId);
    }

    private async Task<int> CreateColumn(HttpClient client, int boardId, string name)
    {
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}