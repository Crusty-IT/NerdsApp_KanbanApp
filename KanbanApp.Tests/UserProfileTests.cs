namespace KanbanApp.Tests;

public class UserProfileTests : TestBase
{
    public UserProfileTests(KanbanWebAppFactory factory) : base(factory)
    {
    }

    [Fact]
    public async Task GetMe_WithoutToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserData()
    {
        var client = await CreateAuthenticatedClientAsync("profile_me@test.com");

        var response = await client.GetAsync("/api/users/me");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("profile_me@test.com", data.GetProperty("email").GetString());
        Assert.True(data.TryGetProperty("id", out _));
        Assert.True(data.TryGetProperty("userName", out _));
    }

    [Fact]
    public async Task UpdateProfile_WithValidBio_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("profile_update@test.com");

        var response = await client.PutAsJsonAsync("/api/users/me", new { bio = "My test bio" });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("My test bio", data.GetProperty("bio").GetString());
    }

    [Fact]
    public async Task UpdateProfile_WithoutToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();

        var response = await client.PutAsJsonAsync("/api/users/me", new { bio = "Bio" });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}