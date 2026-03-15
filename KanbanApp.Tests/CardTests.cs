using System.Net;
using System.Net.Http.Json;
using KanbanApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KanbanApp.Tests;

public class CardTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public CardTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.Contains("DbContext")).ToList();
                foreach (var d in toRemove) services.Remove(d);
                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb7"));
            });
        });
    }

    private async Task<HttpClient> RegisterAndLogin(string email)
    {
        var client = _factory.CreateClient();
        var reg = await client.PostAsJsonAsync("/register", new { email, password = "Test123!" });
        reg.EnsureSuccessStatusCode();
        var login = await client.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email, password = "Test123!" });
        login.EnsureSuccessStatusCode();
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>())
            .GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        return client;
    }

    [Fact]
    public async Task CardLifecycle_AsMember_WorksCorrectly()
    {
        var client = await RegisterAndLogin("cardlife@test.com");

        // Create board
        var board = await client.PostAsJsonAsync("/api/boards", new { boardName = "Card Board" });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // Create 2 columns
        var col1 = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "To Do" });
        var col1Id = (await col1.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var col2 = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "Done" });
        var col2Id = (await col2.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        // CREATE card
        var create = await client.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "My Task", description = "Do stuff", columnId = col1Id });
        Assert.Equal(HttpStatusCode.Created, create.StatusCode);
        var cardData = await create.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardData.GetProperty("id").GetInt32();

        // UPDATE card
        var update = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Updated Task", description = "New desc", columnId = col1Id });
        Assert.Equal(HttpStatusCode.OK, update.StatusCode);
        var updated = await update.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Updated Task", updated.GetProperty("title").GetString());

        // MOVE card to another column
        var move = await client.PutAsJsonAsync($"/api/boards/{boardId}/cards/{cardId}",
            new { title = "Updated Task", description = "New desc", columnId = col2Id });
        Assert.Equal(HttpStatusCode.OK, move.StatusCode);
        var moved = await move.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(col2Id, moved.GetProperty("columnId").GetInt32());

        // DELETE card
        var delete = await client.DeleteAsync($"/api/boards/{boardId}/cards/{cardId}");
        Assert.Equal(HttpStatusCode.NoContent, delete.StatusCode);
    }

    [Fact]
    public async Task CardOperations_AsNonMember_ReturnsForbid()
    {
        var owner = await RegisterAndLogin("cardowner@test.com");
        var board = await owner.PostAsJsonAsync("/api/boards", new { boardName = "Private Board" });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        var col = await owner.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "Col" });
        var colId = (await col.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var outsider = await RegisterAndLogin("cardoutsider@test.com");

        var create = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/cards",
            new { title = "Hack", description = "x", columnId = colId });
        Assert.Equal(HttpStatusCode.Forbidden, create.StatusCode);

        var update = await outsider.PutAsJsonAsync($"/api/boards/{boardId}/cards/1",
            new { title = "Hack", description = "x", columnId = colId });
        Assert.Equal(HttpStatusCode.Forbidden, update.StatusCode);

        var delete = await outsider.DeleteAsync($"/api/boards/{boardId}/cards/1");
        Assert.Equal(HttpStatusCode.Forbidden, delete.StatusCode);
    }
}