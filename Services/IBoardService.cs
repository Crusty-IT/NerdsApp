namespace KanbanApp.Services;

using Models;

/// <summary>
/// Serwis odpowiedzialny za operacje CRUD na tablicach Kanban.
/// </summary>
public interface IBoardService
{
    /// <summary>
    /// Pobiera wszystkie tablice, do których należy dany użytkownik (jako Owner lub Member).
    /// </summary>
    /// <param name="userId">Identyfikator użytkownika (z ASP.NET Core Identity).</param>
    /// <returns>Kolekcja tablic użytkownika.</returns>
    Task<IEnumerable<Board>> GetAllByUserAsync(string userId);

    /// <summary>
    /// Pobiera tablicę po identyfikatorze z walidacją, czy użytkownik ma do niej dostęp.
    /// </summary>
    /// <param name="boardId">Identyfikator tablicy.</param>
    /// <param name="userId">Identyfikator użytkownika żądającego dostępu.</param>
    /// <returns>Tablica, jeśli istnieje i użytkownik ma dostęp; w przeciwnym razie <c>null</c>.</returns>
    Task<Board?> GetByIdAsync(int boardId, string userId);

    /// <summary>
    /// Tworzy nową tablicę i automatycznie dodaje tworzącego użytkownika jako <see cref="BoardRole.Owner"/>.
    /// </summary>
    /// <param name="name">Nazwa tablicy.</param>
    /// <param name="description">Opcjonalny opis tablicy.</param>
    /// <param name="ownerUserId">Identyfikator użytkownika, który zostanie właścicielem.</param>
    /// <returns>Nowo utworzona tablica.</returns>
    Task<Board> CreateAsync(string name, string? description, string ownerUserId);

    /// <summary>
    /// Aktualizuje nazwę i/lub opis istniejącej tablicy.
    /// Użytkownik musi mieć dostęp do tablicy.
    /// </summary>
    /// <param name="boardId">Identyfikator tablicy do aktualizacji.</param>
    /// <param name="userId">Identyfikator użytkownika wykonującego operację.</param>
    /// <param name="name">Nowa nazwa tablicy.</param>
    /// <param name="description">Nowy opis tablicy.</param>
    /// <returns>Zaktualizowana tablica lub <c>null</c>, jeśli tablica nie istnieje lub brak dostępu.</returns>
    Task<Board?> UpdateAsync(int boardId, string userId, string name, string? description);

    /// <summary>
    /// Usuwa tablicę. Operacja dozwolona wyłącznie dla użytkownika z rolą <see cref="BoardRole.Owner"/>.
    /// </summary>
    /// <param name="boardId">Identyfikator tablicy do usunięcia.</param>
    /// <param name="userId">Identyfikator użytkownika wykonującego operację.</param>
    /// <returns><c>true</c>, jeśli tablica została usunięta; <c>false</c>, jeśli nie istnieje lub użytkownik nie jest właścicielem.</returns>
    Task<bool> DeleteAsync(int boardId, string userId);
}