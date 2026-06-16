namespace KanbanApp.Backend.DTOs;

public record UpdateColumnDto(string Name, string? Color, int? Position = null);