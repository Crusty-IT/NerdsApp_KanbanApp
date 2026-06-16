namespace KanbanApp.Tests;

public class ColumnTests : TestBase
{
    public ColumnTests(KanbanWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateColumn_AsMember_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("col_member1@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "To Do" });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task CreateColumn_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("col_owner2@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var outsider = await CreateAuthenticatedClientAsync("col_outsider2@test.com");

        var response = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "Hack" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateColumn_WithEmptyName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("col_empty@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = "" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateColumn_WithTooLongName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("col_longname@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns",
            new { name = new string('x', 101) });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateColumn_AsMember_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("col_member3@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}",
            new { name = "Done" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Done", data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task UpdateColumn_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("col_owner_upd@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var colId = await CreateColumnAsync(owner, boardId, "To Do");
        var outsider = await CreateAuthenticatedClientAsync("col_outsider_upd@test.com");

        var response = await outsider.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}",
            new { name = "Hacked" });

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UpdateColumn_NonExistent_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("col_notfound_upd@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/99999",
            new { name = "Ghost" });

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteColumn_Empty_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync("col_member4@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Delete");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_WithCards_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("col_withcards@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "Has Cards");
        await CreateCardAsync(client, boardId, colId, "Card 1");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task DeleteColumn_NonExistent_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("col_notfound_del@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/columns/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteColumn_WithCards_ClearsCardsAndReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync("col_member5@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "With Cards");

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
        var client = await CreateAuthenticatedClientAsync("col_member6@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "Colored");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}",
            new { name = "Colored", color = "#ef4444" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#ef4444", data.GetProperty("color").GetString());
    }
}
