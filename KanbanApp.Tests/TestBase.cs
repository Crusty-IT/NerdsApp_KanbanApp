namespace KanbanApp.Tests;

public abstract class TestBase : IClassFixture<KanbanWebAppFactory>
{
    protected readonly KanbanWebAppFactory Factory;

    protected TestBase(KanbanWebAppFactory factory)
    {
        Factory = factory;
    }

    protected async Task<HttpClient> CreateAuthenticatedClientAsync(string email, string password = "Test123!")
    {
        var client = Factory.CreateClient();

        await client.PostAsJsonAsync("/register", new { email, password });

        var loginResponse = await client.PostAsJsonAsync(
            "/login",
            new { email, password });

        loginResponse.EnsureSuccessStatusCode();

        var tokenData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenData.GetProperty("accessToken").GetString()!;

        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        return client;
    }

    protected async Task<int> CreateBoardAsync(HttpClient client, string boardName)
    {
        var response = await client.PostAsJsonAsync("/api/boards", new { boardName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    protected async Task<int> CreateColumnAsync(HttpClient client, int boardId, string columnName)
    {
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = columnName });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    protected async Task<int> CreateCardAsync(HttpClient client, int boardId, int columnId, string cardTitle)
    {
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = cardTitle, columnId });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }

    protected async Task<int> CreateProjectAsync(HttpClient client, string projectName, string color = "#00d4ff")
    {
        var response = await client.PostAsJsonAsync("/api/projects", new
        {
            name = projectName,
            description = "Test description",
            color
        });
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
    }
}