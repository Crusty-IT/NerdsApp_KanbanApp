namespace KanbanApp.Tests;

public class AuthTests : IClassFixture<KanbanWebAppFactory>
{
    private readonly HttpClient _client;

    public AuthTests(KanbanWebAppFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/register", new
        {
            email = "auth_register@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        await _client.PostAsJsonAsync("/register", new
        {
            email = "auth_duplicate@test.com",
            password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/register", new
        {
            email = "auth_duplicate@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsTokens()
    {
        await _client.PostAsJsonAsync("/register", new
        {
            email = "auth_login@test.com",
            password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/login", new
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
        await _client.PostAsJsonAsync("/register", new
        {
            email = "auth_wrongpass@test.com",
            password = "Test123!"
        });

        var response = await _client.PostAsJsonAsync("/login", new
        {
            email = "auth_wrongpass@test.com",
            password = "WrongPassword!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithNonExistingEmail_ReturnsUnauthorized()
    {
        var response = await _client.PostAsJsonAsync("/login", new
        {
            email = "auth_ghost@test.com",
            password = "Test123!"
        });

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }
}
