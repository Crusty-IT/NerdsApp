using System.Net;
using System.Net.Http.Json;
using KanbanApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KanbanApp.Tests;

public class ColumnTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly WebApplicationFactory<Program> _factory;

    public ColumnTests(WebApplicationFactory<Program> factory)
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
                    options.UseInMemoryDatabase("TestDb5"));
            });
        });
    }

    private async Task<(HttpClient client, int boardId)> SetupMemberWithBoard(string email)
    {
        var client = _factory.CreateClient();
        await client.PostAsJsonAsync("/register", new { email, password = "Test123!" });
        var login = await client.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email, password = "Test123!" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var board = await client.PostAsJsonAsync("/api/boards", new { boardName = "Test Board" });
        var boardId = (await board.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();
        return (client, boardId);
    }

    [Fact]
    public async Task CreateColumn_AsMember_ReturnsCreated()
    {
        var (client, boardId) = await SetupMemberWithBoard("col1@test.com");
        var response = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "To Do" });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Fact]
    public async Task UpdateColumn_AsMember_ReturnsOk()
    {
        var (client, boardId) = await SetupMemberWithBoard("col2@test.com");
        var create = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "To Do" });
        var colId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var response = await client.PutAsJsonAsync($"/api/boards/{boardId}/columns/{colId}", new { name = "Done" });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task DeleteColumn_Empty_ReturnsNoContent()
    {
        var (client, boardId) = await SetupMemberWithBoard("col3@test.com");
        var create = await client.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "To Do" });
        var colId = (await create.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetInt32();

        var response = await client.DeleteAsync($"/api/boards/{boardId}/columns/{colId}");
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task ColumnEndpoints_AsNonMember_ReturnsForbid()
    {
        var (ownerClient, boardId) = await SetupMemberWithBoard("colowner@test.com");

        var outsider = _factory.CreateClient();
        await outsider.PostAsJsonAsync("/register", new { email = "colout@test.com", password = "Test123!" });
        var login = await outsider.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email = "colout@test.com", password = "Test123!" });
        var token = (await login.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("accessToken").GetString();
        outsider.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var response = await outsider.PostAsJsonAsync($"/api/boards/{boardId}/columns", new { name = "Hack" });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }
}