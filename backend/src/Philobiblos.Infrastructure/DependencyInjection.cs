using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Repositories;

namespace Philobiblos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Library")));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LibraryDbContext>());
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();

        return services;
    }
}
