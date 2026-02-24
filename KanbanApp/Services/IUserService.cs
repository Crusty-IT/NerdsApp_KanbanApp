namespace KanbanApp.Services;

public interface IUserService
{
    Task<UserProfileDto?> GetUserProfileAsync(string userId);
}

public record UserProfileDto(string Id, string? UserName, string? Email);