namespace KanbanApp.Tests;

public class CardTests : TestBase
{
    public CardTests(KanbanWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task CreateCard_AsMember_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("card_create1@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "My Task", columnId = colId });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("My Task", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task CreateCard_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("card_owner2@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var colId = await CreateColumnAsync(owner, boardId, "To Do");
        var outsider = await CreateAuthenticatedClientAsync("card_outsider2@test.com");

        var response = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "Hack", columnId = colId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateCard_WithEmptyTitle_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("card_emptytitle@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "", columnId = colId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateCard_WithTooLongTitle_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("card_longtitle@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");

        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = new string('x', 201), columnId = colId });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateCard_AsMember_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("card_update3@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Old Title");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "New Title", columnId = colId });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Title", data.GetProperty("title").GetString());
    }

    [Fact]
    public async Task UpdateCard_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("card_owner4@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var colId = await CreateColumnAsync(owner, boardId, "To Do");
        var cardId = await CreateCardAsync(owner, boardId, colId, "Task");
        var outsider = await CreateAuthenticatedClientAsync("card_outsider4@test.com");

        var response = await outsider.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Hacked", columnId = colId });

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_AsMember_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClientAsync("card_delete5@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "To Delete");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("card_owner6@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var colId = await CreateColumnAsync(owner, boardId, "To Do");
        var cardId = await CreateCardAsync(owner, boardId, colId, "Protected");
        var outsider = await CreateAuthenticatedClientAsync("card_outsider6@test.com");

        var response = await outsider.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task DeleteCard_NonExistent_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("card_notfound@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");

        var response = await client.DeleteAsync($"/api/boards/{boardId}/cards/99999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task MoveCard_ToDifferentColumn_ReturnsUpdatedColumnId()
    {
        var client = await CreateAuthenticatedClientAsync("card_move7@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var col1Id = await CreateColumnAsync(client, boardId, "Column A");
        var col2Id = await CreateColumnAsync(client, boardId, "Done");
        var cardId = await CreateCardAsync(client, boardId, col1Id, "Movable Task");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Movable Task", columnId = col2Id });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(col2Id, data.GetProperty("columnId").GetInt32());
    }

    [Fact]
    public async Task CreateCard_WithDueDate_ReturnsCorrectData()
    {
        var client = await CreateAuthenticatedClientAsync("card_meta8@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
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
    public async Task SearchCards_WithValidQuery_ReturnsMatchingCards()
    {
        var client = await CreateAuthenticatedClientAsync("card_search1@test.com");
        var boardId = await CreateBoardAsync(client, "Search Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        await CreateCardAsync(client, boardId, colId, "Login page bug");
        await CreateCardAsync(client, boardId, colId, "Unrelated task");

        var response = await client.GetAsync($"/api/boards/{boardId}/cards/search?q=Login");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        data.GetArrayLength().Should().Be(1);
        data[0].GetProperty("title").GetString().Should().Be("Login page bug");
    }

    [Fact]
    public async Task SearchCards_WithEmptyQuery_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("card_search2@test.com");
        var boardId = await CreateBoardAsync(client, "Search Board");

        var response = await client.GetAsync($"/api/boards/{boardId}/cards/search?q=");

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task SearchCards_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("card_search3_owner@test.com");
        var boardId = await CreateBoardAsync(owner, "Private Board");
        var outsider = await CreateAuthenticatedClientAsync("card_search3_outsider@test.com");

        var response = await outsider.GetAsync($"/api/boards/{boardId}/cards/search?q=test");

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadCardImage_AsMember_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("card_image11@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Bug with screenshot");
        using var content = new MultipartFormDataContent();
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var image = new ByteArrayContent(pngBytes);
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "screenshot.png");

        var response = await client.PostAsync($"/api/boards/{boardId}/cards/{cardId}/images", content);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var uploaded = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("image/png", uploaded.GetProperty("contentType").GetString());
    }

    [Fact]
    public async Task UploadCardImage_BoardDetailsContainImage()
    {
        var client = await CreateAuthenticatedClientAsync("card_image11b@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Card with image");
        using var content = new MultipartFormDataContent();
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var image = new ByteArrayContent(pngBytes);
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "screenshot.png");
        await client.PostAsync($"/api/boards/{boardId}/cards/{cardId}/images", content);

        var boardResponse = await client.GetAsync($"/api/boards/{boardId}");
        var board = await boardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var images = board.GetProperty("columns")[0].GetProperty("cards")[0].GetProperty("images");
        Assert.Single(images.EnumerateArray());
    }

    [Fact]
    public async Task UploadCardImage_AsNonMember_ReturnsForbidden()
    {
        var owner = await CreateAuthenticatedClientAsync("card_image_owner@test.com");
        var boardId = await CreateBoardAsync(owner, "Test Board");
        var colId = await CreateColumnAsync(owner, boardId, "To Do");
        var cardId = await CreateCardAsync(owner, boardId, colId, "Card");
        var outsider = await CreateAuthenticatedClientAsync("card_image_outsider@test.com");
        using var content = new MultipartFormDataContent();
        byte[] pngBytes = [0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A];
        var image = new ByteArrayContent(pngBytes);
        image.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png");
        content.Add(image, "file", "screenshot.png");

        var response = await outsider.PostAsync($"/api/boards/{boardId}/cards/{cardId}/images", content);

        response.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task UploadCardImage_WithInvalidFile_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("card_image12@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Invalid image");
        using var content = new MultipartFormDataContent();
        var file = new ByteArrayContent([1, 2, 3]);
        file.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("text/plain");
        content.Add(file, "file", "notes.txt");

        var response = await client.PostAsync($"/api/boards/{boardId}/cards/{cardId}/images", content);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task AssignCard_ToValidMember_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("card_assign9@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Assign Task");

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
        var client = await CreateAuthenticatedClientAsync("card_assign10@test.com");
        var boardId = await CreateBoardAsync(client, "Test Board");
        var colId = await CreateColumnAsync(client, boardId, "To Do");
        var cardId = await CreateCardAsync(client, boardId, colId, "Assign Task 2");

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}/assign",
            new { userId = "nonexistent-user-id" });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
