using Microsoft.EntityFrameworkCore;
using Philobiblos.Api.Endpoints;
using Philobiblos.Application;
using Philobiblos.Infrastructure;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.Middleware;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseSerilog((context, configuration) => configuration
    .ReadFrom.Configuration(context.Configuration));

builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
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

app.UseAuthentication();
app.UseAuthorization();

app.UseSerilogRequestLogging(options =>
{
    options.EnrichDiagnosticContext = (diagnosticContext, httpContext) =>
        diagnosticContext.Set("CorrelationId", httpContext.TraceIdentifier);
});

app.MapAuthEndpoints();
app.MapGenreEndpoints();
app.MapAuthorEndpoints();
app.MapBookEndpoints();

app.Run();

public partial class Program;
