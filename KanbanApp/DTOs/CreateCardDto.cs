namespace KanbanApp.DTOs;
public record CreateCardDto(string Title, string? Description, int ColumnId);