namespace KanbanApi.Models;

public class BoardMember
{

    public string UserId { get; set; } = string.Empty;
    public int BoardId { get; set; }
    

    public ApplicationUser User { get; set; } = null!;
    
    public Board Board { get; set; } = null!;
    public BoardRole Role { get; set; } = BoardRole.Member;
    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
}


public enum BoardRole
{
    Member,
    Owner
}