namespace KanbanApp.Backend.DTOs;

public record BoardDetailDto(
    int Id,
    string Name,
    string? Description,
    string Color,
    string? CoverImageUrl,
    string? CoverObjectPosition,
    DateTime CreatedAt,
    int? ProjectId,
    bool IsOwner,
    List<ColumnDto> Columns
);

public record ColumnDto(
    int Id,
    string Name,
    int Position,
    string Color,
    List<CardDto> Cards
);

public record CardDto(
    int Id,
    string Title,
    string? Description,
    int Position,
    int ColumnId,
    DateTime CreatedAt,
    string? AssignedToUserId,
    DateTime? DueDate,
    int? Priority,
    List<CardImageDto> Images
);

public record CardImageDto(
    int Id,
    string FileName,
    string Url,
    string ContentType,
    string? ObjectPosition,
    DateTime UploadedAt
);
