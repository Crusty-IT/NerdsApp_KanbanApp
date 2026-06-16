using KanbanApp.Backend.Hubs;

namespace KanbanApp.Backend.Extensions;

public static class SignalRExtensions
{
    public static IServiceCollection AddSignalRServices(this IServiceCollection services)
    {
        services.AddSignalR();
        services.AddSingleton<PresenceTracker>();
        return services;
    }

    public static WebApplication MapSignalREndpoints(this WebApplication app)
    {
        app.MapHub<KanbanHub>("/hubs/kanban").RequireAuthorization();
        return app;
    }
}
