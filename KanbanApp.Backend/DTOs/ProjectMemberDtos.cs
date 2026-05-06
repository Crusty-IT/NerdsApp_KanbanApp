namespace KanbanApp.Backend.DTOs;

public record InviteProjectMemberDto(string Email);

public record ProjectMemberDetailDto(
    string UserId,
    string UserName,
    string? ProfilePictureUrl,
    string Role,
    DateTime JoinedAt
);