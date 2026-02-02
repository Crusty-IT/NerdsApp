namespace KanbanApp.Models;

public class Card
{
    public int Id { get; set; }
    
    public string Title { get; set; } = string.Empty;
    
    public string? Description { get; set; }
    
    public int Position { get; set; }
    
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public int ColumnId { get; set; }

    public Column Column { get; set; } = null!;
}
