namespace KanbanApp.Backend.DTOs;

public record CreateBoardDto(string BoardName, int? ProjectId, string? Color);