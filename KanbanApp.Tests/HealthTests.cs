namespace KanbanApp.Tests;

public class HealthTests(KanbanWebAppFactory factory) : IClassFixture<KanbanWebAppFactory>
{
    [Fact]
    public async Task Health_Returns200()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Health_ReturnsHealthyStatus()
    {
        var client = factory.CreateClient();
        var response = await client.GetAsync("/health");
        var json = await response.Content.ReadFromJsonAsync<JsonElement>();
        json.GetProperty("status").GetString().Should().Be("healthy");
    }
}