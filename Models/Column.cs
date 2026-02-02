namespace KanbanApp.Models;

public class Column
{
    public int Id { get; set; }
    
    public string Name { get; set; } = string.Empty;
    
    public int Position { get; set; }
    
    public int BoardId { get; set; }
    

    public Board Board { get; set; } = null!;
    
    public ICollection<Card> Cards { get; set; } = new List<Card>();
}
