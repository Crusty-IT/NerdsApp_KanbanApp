namespace KanbanApp.Backend.DTOs;

public record UpdateCardDto(
    string Title,
    int ColumnId,
    string? Description = null,
    string? AssignedToUserId = null,
    DateTime? DueDate = null,
    int? Priority = null,
    int? Position = null);
