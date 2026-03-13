using Microsoft.EntityFrameworkCore;
using KanbanApp.Data;
using KanbanApp.Models;
using KanbanApp.Services;
using System.Security.Claims;
using KanbanApp.DTOs;
using KanbanApp.Authorization;
using Microsoft.AspNetCore.Authorization;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddIdentityApiEndpoints<ApplicationUser>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddScoped<IBoardService, BoardService>();
builder.Services.AddScoped<IUserService, UserService>();

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("IsBoardOwner", policy =>
        policy.Requirements.Add(new IsBoardOwnerRequirement()));
    options.AddPolicy("IsBoardMember", policy =>
        policy.Requirements.Add(new IsBoardMemberRequirement()));
});

builder.Services.AddScoped<IAuthorizationHandler, IsBoardOwnerHandler>();
builder.Services.AddScoped<IAuthorizationHandler, IsBoardMemberHandler>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapIdentityApi<ApplicationUser>();

app.MapGet("/api/boards", async (IBoardService service) =>
{
    var boards = await service.GetAllByUserAsync("test-user-id");
    return Results.Ok(boards);
});

app.MapGet("/api/users/me", async (ClaimsPrincipal user, IUserService userService) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var profile = await userService.GetUserProfileAsync(userId!);
    if (profile == null) return Results.NotFound();
    return Results.Ok(profile);
}).RequireAuthorization();

app.MapPost("/api/boards", async (CreateBoardDto dto, IBoardService boardService, ClaimsPrincipal user) =>
{
    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var board = await boardService.CreateAsync(dto.BoardName, null, userId!);
    return TypedResults.Created($"/api/boards/{board.Id}", new { board.Id, board.Name, board.Description });
    
}).RequireAuthorization();

app.MapPost("/api/boards/{boardId}/members", async (
    int boardId,
    string userId,
    IAuthorizationService authorizationService,
    ClaimsPrincipal user,
    ApplicationDbContext db) =>
{
    var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardOwner");
    if (!authResult.Succeeded) return Results.Forbid();
    
    var alreadyMember = await db.BoardMembers
        .AnyAsync(m => m.BoardId == boardId && m.UserId == userId);
    if (alreadyMember) return Results.Conflict("User is already a member of this board.");

    var member = new BoardMember { BoardId = boardId, UserId = userId, Role = BoardRole.Member };
    db.BoardMembers.Add(member);
    await db.SaveChangesAsync();

    return Results.Ok(member);
}).RequireAuthorization();

app.MapGet("/api/boards/{boardId}", async (
    int boardId,
    IBoardService boardService,
    IAuthorizationService authorizationService,
    ClaimsPrincipal user) =>
{
    var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();

    var userId = user.FindFirstValue(ClaimTypes.NameIdentifier);
    var board = await boardService.GetByIdAsync(boardId, userId!);
    if (board == null) return Results.NotFound();

    var dto = new BoardDetailDto(
        board.Id,
        board.Name,
        board.Description,
        board.CreatedAt,
        board.Columns.Select(c => new ColumnDto(
            c.Id,
            c.Name,
            c.Position,
            c.Cards.Select(card => new CardDto(
                card.Id,
                card.Title,
                card.Description,
                card.Position,
                card.CreatedAt
            )).ToList()
        )).ToList()
    );

    return Results.Ok(dto);
}).RequireAuthorization();

var columns = app.MapGroup("/api/boards/{boardId}/columns")
    .RequireAuthorization();

columns.MapPost("/", async (int boardId, CreateColumnDto dto, ApplicationDbContext db,
    IAuthorizationService authorizationService, ClaimsPrincipal user) =>
{
    var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();
    
    var position = dto.Position ?? await db.Columns.Where(c => c.BoardId == boardId).CountAsync();
    var column = new Column { Name = dto.Name, Position = position, BoardId = boardId };
    db.Columns.Add(column);
    await db.SaveChangesAsync();
    return TypedResults.Created($"/api/boards/{boardId}/columns/{column.Id}", 
        new { column.Id, column.Name, column.Position });
});

columns.MapPut("/{columnId}", async (int boardId, int columnId, UpdateColumnDto dto, ApplicationDbContext db,
    IAuthorizationService authorizationService, ClaimsPrincipal user) =>
{
    var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();
    
    var column = await db.Columns.FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
    if (column == null) return Results.NotFound();
    column.Name = dto.Name;
    await db.SaveChangesAsync();
    return Results.Ok(new { column.Id, column.Name, column.Position });
});

columns.MapDelete("/{columnId}", async (int boardId, int columnId, ApplicationDbContext db,
    IAuthorizationService authorizationService, ClaimsPrincipal user) =>
{
    var authResult = await authorizationService.AuthorizeAsync(user, boardId, "IsBoardMember");
    if (!authResult.Succeeded) return Results.Forbid();
    
    var column = await db.Columns.Include(c => c.Cards).FirstOrDefaultAsync(c => c.Id == columnId && c.BoardId == boardId);
    if (column == null) return Results.NotFound();
    if (column.Cards.Any()) return Results.BadRequest("Cannot delete column with existing cards.");
    db.Columns.Remove(column);
    await db.SaveChangesAsync();
    return Results.NoContent();
});

app.Run();

public partial class Program { }

