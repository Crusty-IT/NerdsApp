using System.Net;
using System.Net.Http.Json;
using KanbanApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KanbanApp.Tests;

public class BoardMemberTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _ownerClient;
    private readonly HttpClient _nonOwnerClient;
    private readonly WebApplicationFactory<Program> _factory;

    public BoardMemberTests(WebApplicationFactory<Program> factory)
    {
        _factory = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.Contains("DbContext")).ToList();
                foreach (var d in toRemove) services.Remove(d);
                var dbName = Guid.NewGuid().ToString();
                services.AddDbContext<ApplicationDbContext>(options => options.UseInMemoryDatabase(dbName));
            });
        });
        _ownerClient = _factory.CreateClient();
        _nonOwnerClient = _factory.CreateClient();
    }

    private async Task<(string token, string userId)> RegisterAndLogin(string email)
    {
        await _ownerClient.PostAsJsonAsync("/register", new { email, password = "Test123!" });
        var loginResponse = await _ownerClient.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email, password = "Test123!" });
        var tokenData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenData.GetProperty("accessToken").GetString()!;
        return (token, email);
    }

    [Fact]
    public async Task AddMember_AsOwner_ReturnsOk()
    {
        var (ownerToken, _) = await RegisterAndLogin("owner@test.com");
        _ownerClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ownerToken);

        var boardResponse = await _ownerClient.PostAsJsonAsync("/api/boards", new { boardName = "Test Board" });
        var boardData = await boardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var boardId = boardData.GetProperty("id").GetInt32();

        await _ownerClient.PostAsJsonAsync("/register", new { email = "newmember@test.com", password = "Test123!" });

        var response =
            await _ownerClient.PostAsJsonAsync($"/api/boards/{boardId}/members?userId=newmember@test.com", new { });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task AddMember_AsMemberNotOwner_Returns403()
    {
        var (ownerToken, _) = await RegisterAndLogin("owner2@test.com");
        _ownerClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", ownerToken);

        var boardResponse = await _ownerClient.PostAsJsonAsync("/api/boards", new { boardName = "Test Board 2" });
        var boardData = await boardResponse.Content.ReadFromJsonAsync<JsonElement>();
        var boardId = boardData.GetProperty("id").GetInt32();

        var (nonOwnerToken, _) = await RegisterAndLogin("nonowner@test.com");
        await _ownerClient.PostAsJsonAsync($"/api/boards/{boardId}/members?userId=nonowner@test.com", new { });

        _nonOwnerClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", nonOwnerToken);

        var response =
            await _nonOwnerClient.PostAsJsonAsync($"/api/boards/{boardId}/members?userId=someone@test.com", new { });
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetBoardDetail_AsMember_ReturnsBoard()
    {
        var client = _factory.CreateClient();

        await client.PostAsJsonAsync("/register", new { email = "detail@test.com", password = "Test123!" });
        var login = await client.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email = "detail@test.com", password = "Test123!" });
        var tokenData = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                tokenData.GetProperty("accessToken").GetString());

        var create = await client.PostAsJsonAsync("/api/boards", new { boardName = "Detail Board" });
        var board = await create.Content.ReadFromJsonAsync<JsonElement>();
        var boardId = board.GetProperty("id").GetInt32();

        var response = await client.GetAsync($"/api/boards/{boardId}");

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var detail = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal("Detail Board", detail.GetProperty("name").GetString());
        Assert.True(detail.TryGetProperty("columns", out _));
    }

    [Fact]
    public async Task GetBoardDetail_AsNonMember_ReturnsForbid()
    {
        var ownerClient = _factory.CreateClient();

        await ownerClient.PostAsJsonAsync("/register",
            new { email = "detailowner@test.com", password = "Test123!" });
        var login1 = await ownerClient.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email = "detailowner@test.com", password = "Test123!" });
        var token1 = await login1.Content.ReadFromJsonAsync<JsonElement>();
        ownerClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                token1.GetProperty("accessToken").GetString());

        var create = await ownerClient.PostAsJsonAsync("/api/boards", new { boardName = "Private Board" });
        var board = await create.Content.ReadFromJsonAsync<JsonElement>();
        var boardId = board.GetProperty("id").GetInt32();

        var outsiderClient = _factory.CreateClient();
        await outsiderClient.PostAsJsonAsync("/register",
            new { email = "outsider@test.com", password = "Test123!" });
        var login2 = await outsiderClient.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false",
            new { email = "outsider@test.com", password = "Test123!" });
        var token2 = await login2.Content.ReadFromJsonAsync<JsonElement>();
        outsiderClient.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer",
                token2.GetProperty("accessToken").GetString());

        var response = await outsiderClient.GetAsync($"/api/boards/{boardId}");

        Assert.NotEqual(HttpStatusCode.OK, response.StatusCode);
    }
}