namespace KanbanApp.Services;

using Data;
using Models;
using Microsoft.EntityFrameworkCore;

public class BoardService : IBoardService
{
    private readonly ApplicationDbContext _context;

    public BoardService(ApplicationDbContext context)
    {
        _context = context;
    }

    public async Task<IEnumerable<Board>> GetAllByUserAsync(string userId)
    {
        return await _context.Boards
            .Where(b => b.BoardMembers.Any(bm => bm.UserId == userId))
            .Include(b => b.Columns)
            .OrderByDescending(b => b.CreatedAt)
            .ToListAsync();
    }

    public Task<Board?> GetByIdAsync(int boardId, string userId)
    {
        throw new NotImplementedException();
    }

    public Task<Board> CreateAsync(string name, string? description, string ownerUserId)
    {
        throw new NotImplementedException();
    }

    public Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description)
    {
        throw new NotImplementedException();
    }

    public Task<bool> DeleteAsync(int boardId, string userId)
    {
        throw new NotImplementedException();
    }
}