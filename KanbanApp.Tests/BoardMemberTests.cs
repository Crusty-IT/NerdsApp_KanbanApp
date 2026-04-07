namespace KanbanApp.Tests;

public class BoardMemberTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public BoardMemberTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task AddMember_AsOwner_ReturnsOk()
    {
        var owner = await CreateAuthenticatedClient("bm_owner1@test.com");
        var boardId = await CreateBoard(owner, "Member Board");

        await _factory.CreateClient().PostAsJsonAsync("/register",
            new { email = "bm_newmember1@test.com", password = "Test123!" });

        var response = await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_newmember1@test.com" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_AsNonOwner_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bm_owner2@test.com");
        var boardId = await CreateBoard(owner, "Restricted Board");

        var member = await CreateAuthenticatedClient("bm_member2@test.com");
        await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_member2@test.com" });

        await _factory.CreateClient().PostAsJsonAsync("/register",
            new { email = "bm_target2@test.com", password = "Test123!" });

        var response = await member.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_target2@test.com" });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_DuplicateInvite_ReturnsConflict()
    {
        var owner = await CreateAuthenticatedClient("bm_owner3@test.com");
        var boardId = await CreateBoard(owner, "Dup Board");

        await _factory.CreateClient().PostAsJsonAsync("/register",
            new { email = "bm_dupmember3@test.com", password = "Test123!" });

        await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_dupmember3@test.com" });

        var response = await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_dupmember3@test.com" });

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_NonExistingUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClient("bm_owner4@test.com");
        var boardId = await CreateBoard(owner, "Ghost Board");

        var response = await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_ghost@nonexistent.com" });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardDetail_AsMember_ReturnsBoard()
    {
        var owner = await CreateAuthenticatedClient("bm_owner5@test.com");
        var boardId = await CreateBoard(owner, "Detail Board");

        var response = await owner.GetAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Detail Board", data.GetProperty("name").GetString());
        Assert.True(data.TryGetProperty("columns", out _));
    }

    [Fact]
    public async Task GetBoardDetail_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClient("bm_owner6@test.com");
        var boardId = await CreateBoard(owner, "Private Board");

        var outsider = await CreateAuthenticatedClient("bm_outsider6@test.com");

        var response = await outsider.GetAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardMembers_AsOwner_ReturnsMembersList()
    {
        var owner = await CreateAuthenticatedClient("bm_owner7@test.com");
        var boardId = await CreateBoard(owner, "Members Board");

        await _factory.CreateClient().PostAsJsonAsync("/register",
            new { email = "bm_member7@test.com", password = "Test123!" });
        await owner.PostAsJsonAsync($"/api/boards/{boardId}/members",
            new { email = "bm_member7@test.com" });

        var response = await owner.GetAsync($"/api/boards/{boardId}/members");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, data.GetArrayLength());
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