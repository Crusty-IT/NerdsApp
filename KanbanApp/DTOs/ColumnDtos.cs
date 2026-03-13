namespace KanbanApp.DTOs;

public record CreateColumnDto(string Name, int? Position);
public record UpdateColumnDto(string Name);