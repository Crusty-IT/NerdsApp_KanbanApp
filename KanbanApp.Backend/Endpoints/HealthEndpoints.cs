namespace KanbanApp.Backend.Endpoints;

public static class HealthEndpoints
{
    public static void MapHealthEndpoints(this WebApplication app)
    {
        var healthHandler = () => Results.Ok(new
        {
            status = "healthy",
            timestamp = DateTime.UtcNow
        });

        app.MapGet("/health", healthHandler);
        app.MapGet("/api/ping", healthHandler);
    }
}
