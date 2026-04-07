namespace KanbanApp.Tests;

public class BoardOwnerTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public BoardOwnerTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task UpdateBoard_AsOwner_ReturnsOk()
    {
        var client = await CreateAuthenticatedClient("bo_owner1@test.com");
        var boardId = await CreateBoard(client, "Old Name");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}", new { name = "New Name" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Name", data.GetProperty("name").GetString());
    }

    [Fact]
    public async Task DeleteBoard_AsOwner_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClient("bo_owner2@test.com");
        var boardId = await CreateBoard(client, "To Delete");

        var response = await client.DeleteAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoard_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bo_owner3@test.com");
        var boardId = await CreateBoard(owner, "Owner Board");

        var nonMember = await CreateAuthenticatedClient("bo_nonmember3@test.com");

        var response = await nonMember.PutAsJsonAsync($"/api/boards/{boardId}", new { name = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoard_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bo_owner4@test.com");
        var boardId = await CreateBoard(owner, "Owner Board 2");

        var nonMember = await CreateAuthenticatedClient("bo_nonmember4@test.com");

        var response = await nonMember.DeleteAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task UpdateBoard_AsInvitedMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bo_owner5@test.com");
        var boardId = await CreateBoard(owner, "Secured Board");

        var member = await CreateAuthenticatedClient("bo_member5@test.com");
        await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bo_member5@test.com" });

        var response = await member.PutAsJsonAsync($"/api/boards/{boardId}", new { name = "Hacked" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteBoard_AsInvitedMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bo_owner6@test.com");
        var boardId = await CreateBoard(owner, "Secured Board 2");

        var member = await CreateAuthenticatedClient("bo_member6@test.com");
        await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bo_member6@test.com" });

        var response = await member.DeleteAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
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

    private async Task<int> CreateBoard(HttpClient client, string boardName)
    {
        var response = await client.PostAsJsonAsync("/api/boards", new { boardName });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}