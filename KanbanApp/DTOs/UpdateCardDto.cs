namespace KanbanApp.DTOs;
public record UpdateCardDto(string Title, string? Description, int ColumnId);