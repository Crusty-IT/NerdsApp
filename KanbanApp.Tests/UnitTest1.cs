using System.Net;
using System.Net.Http.Json;
using KanbanApp.Data;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Text.Json;

namespace KanbanApp.Tests;

public class AuthTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public AuthTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d => 
                    d.ServiceType.FullName != null && 
                    d.ServiceType.FullName.Contains("DbContext")).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task Register_WithValidData_ReturnsOk()
    {
        var response = await _client.PostAsJsonAsync("/register", new
        {
            email = "test@test.com",
            password = "Test123!"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Login_WithValidCredentials_ReturnsOk()
    {
        await _client.PostAsJsonAsync("/register", new
        {
            email = "login@test.com",
            password = "Test123!"
        });
        var response = await _client.PostAsJsonAsync("/login", new
        {
            email = "login@test.com",
            password = "Test123!"
        });
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Register_WithDuplicateEmail_ReturnsBadRequest()
    {
        await _client.PostAsJsonAsync("/register", new
        {
            email = "duplicate@test.com",
            password = "Test123!"
        });
        var response = await _client.PostAsJsonAsync("/register", new
        {
            email = "duplicate@test.com",
            password = "Test123!"
        });
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}


public class UserProfileTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public UserProfileTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.WithWebHostBuilder(builder =>
        {
            builder.ConfigureServices(services =>
            {
                var toRemove = services.Where(d =>
                    d.ServiceType.FullName != null &&
                    d.ServiceType.FullName.Contains("DbContext")).ToList();
                foreach (var d in toRemove) services.Remove(d);

                services.AddDbContext<ApplicationDbContext>(options =>
                    options.UseInMemoryDatabase("TestDb2"));
            });
        }).CreateClient();
    }

    [Fact]
    public async Task GetMe_WithoutToken_Returns401()
    {
        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetMe_WithValidToken_ReturnsUserData()
    {
        await _client.PostAsJsonAsync("/register", new
        {
            email = "me@test.com",
            password = "Test123!"
        });
        
        var loginResponse = await _client.PostAsJsonAsync("/login?useCookies=false&useSessionCookies=false", new
        {
            email = "me@test.com",
            password = "Test123!"
        });
        
        var tokenData = await loginResponse.Content.ReadFromJsonAsync<JsonElement>();
        var token = tokenData.GetProperty("accessToken").GetString();
        
        _client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);
        
        var response = await _client.GetAsync("/api/users/me");
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }
}