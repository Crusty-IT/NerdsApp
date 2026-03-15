namespace KanbanApp.Services;
using Data;
using Models;
using Microsoft.EntityFrameworkCore;

public class CardService : ICardService
{
    private readonly ApplicationDbContext _context;
    public CardService(ApplicationDbContext context) { _context = context; }

    public async Task<Card?> CreateAsync(int boardId, int columnId, string title, string? description)
    {
        var column = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (column == null) return null;

        var card = new Card { Title = title, Description = description, ColumnId = columnId };
        _context.Cards.Add(card);
        await _context.SaveChangesAsync();
        return card;
    }

    public async Task<Card?> UpdateAsync(int boardId, int cardId, string title, string? description, int columnId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return null;

        var targetColumn = await _context.Columns
            .FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
        if (targetColumn == null) return null;

        card.Title = title;
        card.Description = description;
        card.ColumnId = columnId;
        await _context.SaveChangesAsync();
        return card;
    }

    public async Task<bool> DeleteAsync(int boardId, int cardId)
    {
        var card = await _context.Cards
            .Include(c => c.Column)
            .FirstOrDefaultAsync(c => c.Id == cardId && c.Column.BoardId == boardId);
        if (card == null) return false;

        _context.Cards.Remove(card);
        await _context.SaveChangesAsync();
        return true;
    }
}