namespace KanbanApp.Tests;

public class ProjectTests : TestBase
{
    public ProjectTests(KanbanWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task GetProjects_WithoutAuth_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_WithNoProjects_ReturnsEmptyList()
    {
        var client = await CreateAuthenticatedClientAsync("proj_empty1@test.com");

        var response = await client.GetAsync("/api/projects");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task CreateProject_WithValidData_ReturnsCreated()
    {
        var client = await CreateAuthenticatedClientAsync("proj_create2@test.com");

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
        var client = Factory.CreateClient();

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
        var client = await CreateAuthenticatedClientAsync("proj_color3@test.com");

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
    public async Task CreateProject_WithEmptyName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("proj_empty_name@test.com");

        var response = await client.PostAsJsonAsync("/api/projects", new { name = "", description = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateProject_WithTooLongName_ReturnsBadRequest()
    {
        var client = await CreateAuthenticatedClientAsync("proj_long_name@test.com");

        var response = await client.PostAsJsonAsync("/api/projects", new { name = new string('x', 101), description = "x" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetProjects_ReturnsOnlyOwnProjects()
    {
        var user1 = await CreateAuthenticatedClientAsync("proj_user1_4@test.com");
        var user2 = await CreateAuthenticatedClientAsync("proj_user2_4@test.com");

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
        var client = await CreateAuthenticatedClientAsync("proj_getid5@test.com");
        var projectId = await CreateProjectAsync(client, "Detail Project");

        var response = await client.GetAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Detail Project", data.GetProperty("name").GetString());
        Assert.True(data.TryGetProperty("boards", out _));
    }

    [Fact]
    public async Task GetProjectById_AsOtherUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_owner6@test.com");
        var projectId = await CreateProjectAsync(owner, "Private Project");

        var other = await CreateAuthenticatedClientAsync("proj_other6@test.com");
        var response = await other.GetAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjectById_NonExisting_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("proj_notfound7@test.com");

        var response = await client.GetAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task UpdateProject_AsOwner_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("proj_update8@test.com");
        var projectId = await CreateProjectAsync(client, "Old Name");

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
        var owner = await CreateAuthenticatedClientAsync("proj_owner9@test.com");
        var projectId = await CreateProjectAsync(owner, "Protected Project");

        var other = await CreateAuthenticatedClientAsync("proj_other9@test.com");
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
        var owner = await CreateAuthenticatedClientAsync("proj_owner10@test.com");
        var projectId = await CreateProjectAsync(owner, "Auth Project");

        var anonymous = Factory.CreateClient();
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
        var client = await CreateAuthenticatedClientAsync("proj_delete11@test.com");
        var projectId = await CreateProjectAsync(client, "To Delete");

        var response = await client.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_AsOtherUser_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_owner12@test.com");
        var projectId = await CreateProjectAsync(owner, "Protected Project 2");

        var other = await CreateAuthenticatedClientAsync("proj_other12@test.com");
        var response = await other.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_WithoutAuth_ReturnsUnauthorized()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_owner13@test.com");
        var projectId = await CreateProjectAsync(owner, "Auth Project 2");

        var anonymous = Factory.CreateClient();
        var response = await anonymous.DeleteAsync($"/api/projects/{projectId}");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteProject_NonExisting_ReturnsNotFound()
    {
        var client = await CreateAuthenticatedClientAsync("proj_notfound14@test.com");

        var response = await client.DeleteAsync("/api/projects/99999");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetProjects_AfterDelete_ReturnsUpdatedList()
    {
        var client = await CreateAuthenticatedClientAsync("proj_afterdelete15@test.com");
        var projectId = await CreateProjectAsync(client, "Temp Project");

        await client.DeleteAsync($"/api/projects/{projectId}");

        var response = await client.GetAsync("/api/projects");
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(0, data.GetArrayLength());
    }

    [Fact]
    public async Task InviteProjectMember_AsOwner_ReturnsOk()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_invite_owner@test.com");
        var projectId = await CreateProjectAsync(owner, "Invite Test Project");
        await CreateAuthenticatedClientAsync("proj_invite_target@test.com");

        var response = await owner.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new { email = "proj_invite_target@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task InviteProjectMember_SelfInvite_ReturnsBadRequest()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_selfinvite@test.com");
        var projectId = await CreateProjectAsync(owner, "Self Invite Project");

        var response = await owner.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new { email = "proj_selfinvite@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        data.GetProperty("message").GetString().Should().Contain("yourself");
    }

    [Fact]
    public async Task InviteProjectMember_Duplicate_ReturnsBadRequest()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_dupinvite_owner@test.com");
        var projectId = await CreateProjectAsync(owner, "Duplicate Invite Project");
        await CreateAuthenticatedClientAsync("proj_dupinvite_target@test.com");

        await owner.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new { email = "proj_dupinvite_target@test.com" });
        var response = await owner.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new { email = "proj_dupinvite_target@test.com" });

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        data.GetProperty("message").GetString().Should().Contain("already");
    }

    [Fact]
    public async Task RemoveProjectMember_AsOwner_ReturnsNoContent()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_remove_owner@test.com");
        var projectId = await CreateProjectAsync(owner, "Remove Member Project");
        var member = await CreateAuthenticatedClientAsync("proj_remove_target@test.com");

        await owner.PostAsJsonAsync($"/api/projects/{projectId}/members",
            new { email = "proj_remove_target@test.com" });

        var memberProfile = await member.GetAsync("/api/users/me");
        var memberId = (await memberProfile.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetString();

        var response = await owner.DeleteAsync($"/api/projects/{projectId}/members/{memberId}");

        response.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task GetProjectMembers_AsNonMember_ReturnsNotFound()
    {
        var owner = await CreateAuthenticatedClientAsync("proj_members_owner@test.com");
        var projectId = await CreateProjectAsync(owner, "Members Project");
        var outsider = await CreateAuthenticatedClientAsync("proj_members_outsider@test.com");

        var response = await outsider.GetAsync($"/api/projects/{projectId}/members");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
