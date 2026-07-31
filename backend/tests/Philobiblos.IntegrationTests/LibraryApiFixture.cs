using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Philobiblos.Infrastructure.Data;
using Testcontainers.PostgreSql;

namespace Philobiblos.IntegrationTests;

/// <summary>
/// Shared test infrastructure for the library API integration test suite.
///
/// Isolation strategy:
/// - A single PostgreSQL container is started once for the whole test assembly and shared
///   by all test classes. This keeps the suite fast while still exercising real Npgsql/EF behavior.
/// - EF Core migrations are applied once when the fixture initializes.
/// - Each test class implements <see cref="IAsyncLifetime"/> and calls <see cref="ResetDatabaseAsync"/>
///   in its <c>InitializeAsync</c> method. This truncates all tables in FK-safe order so every
///   test class begins from a known empty state (per-class data isolation).
/// - Tests within a class execute sequentially (xUnit's default behavior inside a class).
///
/// Truncating is chosen over recreating the schema per class because it is fast and preserves
/// the migrated schema; it avoids the cost of repeatedly running migrations while still giving
/// each class a clean database.
/// </summary>
public sealed class LibraryApiFixture : IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("postgres:16-alpine")
        .WithDatabase("philobiblos")
        .WithUsername("postgres")
        .WithPassword("postgres")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public async Task InitializeAsync()
    {
        await _postgres.StartAsync();

        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(builder =>
            {
                builder.UseSetting("ConnectionStrings:Library", _postgres.GetConnectionString());
                builder.UseSetting("Auth:Google:Enabled", "false");
                builder.UseSetting("Auth:Test:Enabled", "true");

                builder.ConfigureServices(services =>
                {
                    services.AddSingleton<IStartupFilter, TestEndpointStartupFilter>();
                });
            });

        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
        await db.Database.MigrateAsync();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Factory.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();

        // Truncate all tables together with CASCADE so the RESTRICT foreign keys are bypassed
        // and every test class starts from a clean, migrated database.
        await db.Database.ExecuteSqlRawAsync("""
            TRUNCATE TABLE "Books", "Authors", "Genres", "Users" CASCADE;
            """);
    }

    public async Task DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }
}

public sealed class TestEndpointStartupFilter : IStartupFilter
{
    public Action<IApplicationBuilder> Configure(Action<IApplicationBuilder> next) =>
        app =>
        {
            next(app);

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapGet("/api/test/throw", () =>
                {
                    throw new InvalidOperationException("Intentional test exception.");
                });
            });
        };
}
