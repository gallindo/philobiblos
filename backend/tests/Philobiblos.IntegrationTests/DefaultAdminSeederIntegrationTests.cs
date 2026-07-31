using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Philobiblos.Application.Users.Dtos;
using Testcontainers.PostgreSql;

namespace Philobiblos.IntegrationTests;

public sealed class DefaultAdminSeederIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("philobiblos")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private HttpClient _client = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        _factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Library", _postgres.GetConnectionString());
                builder.UseSetting("Auth:Google:Enabled", "false");
                builder.UseSetting("Auth:Test:Enabled", "false");
                builder.UseSetting("Auth:DefaultAdmin:Enabled", "true");
                builder.UseSetting("Auth:DefaultAdmin:Email", "admin@example.com");
                builder.UseSetting("Auth:DefaultAdmin:Password", "Admin123!");
                builder.UseSetting("Auth:DefaultAdmin:Roles", "Admin,Editor");

            });

        using var scope = _factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<Philobiblos.Infrastructure.Data.LibraryDbContext>();
        await db.Database.MigrateAsync();

        _client = _factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public async Task DisposeAsync()
    {
        _client.Dispose();
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    [Fact]
    public async Task Seeder_creates_default_admin_and_allows_login()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new
        {
            email = "admin@example.com",
            password = "Admin123!"
        }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user.Should().NotBeNull();
        user!.Roles.Should().Contain("Admin");
        user.Roles.Should().Contain("Editor");
    }
}
