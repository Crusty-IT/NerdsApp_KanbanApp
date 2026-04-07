namespace KanbanApp.Tests;

public class BoardTests : TestBase
{
    public BoardTests(KanbanWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task CreateBoard_WithValidData_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("board_create@test.com");
        var boardId = await CreateBoardAsync(client, "Create Board");

        Assert.True(boardId > 0);
    }

    [Fact]
    public async Task CreateBoard_WithoutAuth_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/boards", new { boardName = "Unauthorized Board" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetBoards_ReturnsOnlyUserBoards()
    {
        var client = await CreateAuthenticatedClientAsync("board_list@test.com");

        await CreateBoardAsync(client, "Board A");
        await CreateBoardAsync(client, "Board B");

        var response = await client.GetAsync("/api/boards");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var boards = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(2, boards.GetArrayLength());
    }

    [Fact]
    public async Task GetBoards_WithoutAuth_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/boards");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateBoard_AssignsBoardOwnerRole()
    {
        var client = await CreateAuthenticatedClientAsync("board_owner_role@test.com");
        var boardId = await CreateBoardAsync(client, "Role Board");

        var members = await client.GetAsync($"/api/boards/{boardId}/members");
        Assert.Equal(HttpStatusCode.OK, members.StatusCode);

        var data = await members.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("Owner", data[0].GetProperty("role").GetString());
    }
}