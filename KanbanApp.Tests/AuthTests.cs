namespace KanbanApp.Tests;

public class AuthTests : TestBase
{
    public AuthTests(KanbanWebAppFactory factory) : base(factory) { }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/register", new
        {
            email = "auth_register@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        var client = Factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email = "auth_duplicate@test.com", password = "Test123!" });

        var response = await client.PostAsJsonAsync("/register", new
        {
            email = "auth_duplicate@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        var client = Factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email = "auth_login@test.com", password = "Test123!" });

        var response = await client.PostAsJsonAsync("/login", new
        {
            email = "auth_login@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(data.TryGetProperty("accessToken", out var token));
        Assert.False(string.IsNullOrEmpty(token.GetString()));
    }

    [Fact]
    public async Task Login_WithWrongPassword_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email = "auth_wrongpass@test.com", password = "Test123!" });

        var response = await client.PostAsJsonAsync("/login", new
        {
            email = "auth_wrongpass@test.com",
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistingEmail_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/login", new
        {
            email = "auth_ghost@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithValidToken_ReturnsNewTokens()
    {
        var client = Factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email = "auth_refresh1@test.com", password = "Test123!" });
        var loginRes = await client.PostAsJsonAsync("/login", new { email = "auth_refresh1@test.com", password = "Test123!" });
        var loginData = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;

        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var data = await response.Content.ReadFromJsonAsync<JsonElement>();
        data.TryGetProperty("accessToken", out var newAccess).Should().BeTrue();
        data.TryGetProperty("refreshToken", out var newRefresh).Should().BeTrue();
        newAccess.GetString().Should().NotBeNullOrEmpty();
        newRefresh.GetString().Should().NotBe(refreshToken);
    }

    [Fact]
    public async Task RefreshToken_WithRevokedToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email = "auth_refresh2@test.com", password = "Test123!" });
        var loginRes = await client.PostAsJsonAsync("/login", new { email = "auth_refresh2@test.com", password = "Test123!" });
        var loginData = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;

        await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });
        var secondResponse = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken });

        secondResponse.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/refresh", new { refreshToken = "invalid-token-value" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_WithValidToken_ReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("auth_logout1@test.com");
        var loginRes = await Factory.CreateClient().PostAsJsonAsync("/login", new { email = "auth_logout1@test.com", password = "Test123!" });
        var loginData = await loginRes.Content.ReadFromJsonAsync<JsonElement>();
        var refreshToken = loginData.GetProperty("refreshToken").GetString()!;

        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithInvalidToken_StillReturnsOk()
    {
        var client = await CreateAuthenticatedClientAsync("auth_logout2@test.com");
        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "nonexistent-token" });

        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Logout_WithoutAuth_ReturnsUnauthorized()
    {
        var client = Factory.CreateClient();
        var response = await client.PostAsJsonAsync("/api/auth/logout", new { refreshToken = "any-token" });

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
