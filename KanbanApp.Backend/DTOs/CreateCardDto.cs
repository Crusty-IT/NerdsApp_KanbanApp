namespace KanbanApp.Backend.DTOs;

public record CreateCardDto(string Title, int ColumnId, string? Description = null, DateTime? DueDate = null, string? Color = null);