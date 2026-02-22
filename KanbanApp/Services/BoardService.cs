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

    public async Task<Board?> GetByIdAsync(int boardId, string userId)
    {
        return await _context.Boards
            .Include(b => b.Columns)
            .FirstOrDefaultAsync(b => b.Id == boardId 
                                      && b.BoardMembers.Any(bm => bm.UserId == userId));
    }

    public async Task<Board> CreateAsync(string name, string? description, string ownerUserId)
    {
        var board = new Board { Name = name, Description = description };
    
        _context.Boards.Add(board);
        _context.BoardMembers.Add(new BoardMember { Board = board, UserId = ownerUserId, Role = BoardRole.Owner });
    
        await _context.SaveChangesAsync();
        return board;
    }

    public async Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description)
    {
        var board = await GetByIdAsync(boardId, userId);
        if (board == null) return null;

        board.Name = name;
        board.Description = description;

        await _context.SaveChangesAsync();
        return board;
    }

    public async Task<bool> DeleteAsync(int boardId, string userId)
    {
        var board = await GetByIdAsync(boardId, userId);
        if (board == null) return false;

        _context.Boards.Remove(board);
        await _context.SaveChangesAsync();
        return true;
    }
}