using Microsoft.AspNetCore.Identity;

namespace KanbanApp.Models;

public class ApplicationUser : IdentityUser
{
    public string? ProfilePictureUrl { get; set; }
    public string? Bio { get; set; }
    
    public ICollection<BoardMember> BoardMembers { get; set; } = new List<BoardMember>();
}

