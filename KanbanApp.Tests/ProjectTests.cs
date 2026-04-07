namespace KanbanApp.Tests;

public class ProjectTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly KanbanWebAppFactory _factory;

    public ProjectTests(KanbanWebAppFactory factory)
    {
        _factory = factory;
    }

    [Fact]
    public async Task GetProjects_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithNoProjects_ReturnsEmptyList()
    {
        var client = await CreateAuthenticatedClient("proj_empty1@test.com");

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClient("proj_create2@test.com");

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "My Project",
            description = "Test description",
            color = "#00d4ff"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("My Project", data.GetProperty("name").GetString());
        Assert.Equal("#00d4ff", data.GetProperty("color").GetString());
        Assert.True(data.TryGetProperty("id", out _));
    }

    [Fact]
    public async Task CreateProject_WithoutAuth_ReturnsUnauthorized()
    {
        var client = _factory.CreateClient();

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "Hack Project",
            description = "x",
            color = "#000000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateProject_WithDefaultColor_UsesDefaultColor()
    {
        var client = await CreateAuthenticatedClient("proj_color3@test.com");

        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = "No Color Project",
            description = "desc"
        });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("#00d4ff", data.GetProperty("color").GetString());
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyOwnProjects()
    {
        var user1 = await CreateAuthenticatedClient("proj_user1_4@test.com");
        var user2 = await CreateAuthenticatedClient("proj_user2_4@test.com");

        await user1.PostAsJsonAsync("/api/projects", new { name = "User1 Project", description = "x", color = "#00d4ff" });
        await user2.PostAsJsonAsync("/api/projects", new { name = "User2 Project", description = "x", color = "#00d4ff" });

        var response = await user1.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(1, data.GetArrayLength());
        Assert.Equal("User1 Project", data[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task GetProjectById_AsOwner_ReturnsProject()
    {
        var client = await CreateAuthenticatedClient("proj_getid5@test.com");
        var projectId = await CreateProject(client, "Detail Project");

        var response = await client.GetAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Detail Project", data.GetProperty("name").GetString());
        Assert.True(data.TryGetProperty("boards", out _));
    }

    [Fact]
    public async Task GetProjectById_AsOtherUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClient("proj_owner6@test.com");
        var projectId = await CreateProject(owner, "Private Project");

        var other = await CreateAuthenticatedClient("proj_other6@test.com");
        var response = await other.GetAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_NonExisting_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClient("proj_notfound7@test.com");

        var response = await client.GetAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_AsOwner_ReturnsOk()
    {
        var client = await CreateAuthenticatedClient("proj_update8@test.com");
        var projectId = await CreateProject(client, "Old Name");

        var response = await client.PutAsJsonAsync($"/api/projects/{projectId}", new
        {
            name = "New Name",
            description = "New desc",
            color = "#ef4444"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("New Name", data.GetProperty("name").GetString());
        Assert.Equal("#ef4444", data.GetProperty("color").GetString());
    }

    [Fact]
    public async Task UpdateProject_AsOtherUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClient("proj_owner9@test.com");
        var projectId = await CreateProject(owner, "Protected Project");

        var other = await CreateAuthenticatedClient("proj_other9@test.com");
        var response = await other.PutAsJsonAsync($"/api/projects/{projectId}", new
        {
            name = "Hacked",
            description = "x",
            color = "#000000"
        });

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_WithoutAuth_ReturnsUnauthorized()
    {
        var owner = await CreateAuthenticatedClient("proj_owner10@test.com");
        var projectId = await CreateProject(owner, "Auth Project");

        var anonymous = _factory.CreateClient();
        var response = await anonymous.PutAsJsonAsync($"/api/projects/{projectId}", new
        {
            name = "Hacked",
            description = "x",
            color = "#000000"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_AsOwner_ReturnsNoContent()
    {
        var client = await CreateAuthenticatedClient("proj_delete11@test.com");
        var projectId = await CreateProject(client, "To Delete");

        var response = await client.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_AsOtherUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClient("proj_owner12@test.com");
        var projectId = await CreateProject(owner, "Protected Project 2");

        var other = await CreateAuthenticatedClient("proj_other12@test.com");
        var response = await other.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_ReturnsUnauthorized()
    {
        var owner = await CreateAuthenticatedClient("proj_owner13@test.com");
        var projectId = await CreateProject(owner, "Auth Project 2");

        var anonymous = _factory.CreateClient();
        var response = await anonymous.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_NonExisting_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClient("proj_notfound14@test.com");

        var response = await client.DeleteAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_AfterDelete_ReturnsUpdatedList()
    {
        var client = await CreateAuthenticatedClient("proj_afterdelete15@test.com");
        var projectId = await CreateProject(client, "Temp Project");

        await client.DeleteAsync($"/api/projects/{projectId}");

        var response = await client.GetAsync("/api/projects");
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, data.GetArrayLength());
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

    private async Task<int> CreateProject(HttpClient client, string name)
    {
        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name,
            description = "Test description",
            color = "#00d4ff"
        });
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}