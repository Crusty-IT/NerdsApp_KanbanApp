using KanbanApp.Backend.Authorization;
using KanbanApp.Backend.Services;
using Microsoft.AspNetCore.Authorization;

namespace KanbanApp.Backend.Extensions;

public static class AppServicesExtensions
{
    public static IServiceCollection AddAppServices(this IServiceCollection services)
    {
        services.AddScoped<IBoardService, BoardService>();
        services.AddScoped<IUserService, UserService>();
        services.AddScoped<ICardService, CardService>();

        services.AddAuthorization(options =>
        {
            options.AddPolicy("IsBoardOwner", policy =>
                policy.Requirements.Add(new IsBoardOwnerRequirement()));
            options.AddPolicy("IsBoardMember", policy =>
                policy.Requirements.Add(new IsBoardMemberRequirement()));
        });

        services.AddScoped<IAuthorizationHandler, IsBoardOwnerHandler>();
        services.AddScoped<IAuthorizationHandler, IsBoardMemberHandler>();

        return services;
    }
}