using FluentValidation;
using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Data;
using Philobiblos.Api.Features.Authors;
using Philobiblos.Api.Features.Books;
using Philobiblos.Api.Features.Genres;
using Philobiblos.Api.Infrastructure;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddDbContext<LibraryDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Library")));

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    using var scope = app.Services.CreateScope();
    var dbContext = scope.ServiceProvider.GetRequiredService<LibraryDbContext>();
    await dbContext.Database.MigrateAsync();
}

app.UseMiddleware<ExceptionHandlingMiddleware>();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
});

var genres = app.MapGroup("/api/genres").WithTags("Genres");
genres.MapCreateGenre();
genres.MapListGenres();
genres.MapGetGenre();
genres.MapUpdateGenre();
genres.MapDeleteGenre();

var authors = app.MapGroup("/api/authors").WithTags("Authors");
authors.MapCreateAuthor();
authors.MapListAuthors();
authors.MapGetAuthor();
authors.MapUpdateAuthor();
authors.MapDeleteAuthor();

var books = app.MapGroup("/api/books").WithTags("Books");
books.MapCreateBook();
books.MapListBooks();
books.MapGetBook();
books.MapUpdateBook();
books.MapDeleteBook();

app.Run();

public partial class Program;
