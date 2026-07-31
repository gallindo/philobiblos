using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Npgsql;
using Philobiblos.Domain.Exceptions;

namespace Philobiblos.Infrastructure.Middleware;

public sealed class ExceptionHandlingMiddleware(
    RequestDelegate next,
    ILogger<ExceptionHandlingMiddleware> logger)
{
    private static readonly JsonSerializerOptions SerializerOptions = new(JsonSerializerDefaults.Web);

    public async Task InvokeAsync(HttpContext context)
    {
        try
        {
            await next(context);
        }
        catch (Exception exception) when (exception is not OperationCanceledException)
        {
            if (context.Response.HasStarted)
            {
                throw;
            }

            await WriteProblemAsync(context, exception);
        }
    }

    private async Task WriteProblemAsync(HttpContext context, Exception exception)
    {
        var (status, title, detail) = exception switch
        {
            NotFoundException => (StatusCodes.Status404NotFound, "Resource Not Found", exception.Message),
            ConflictException => (StatusCodes.Status409Conflict, "Conflict", exception.Message),
            BadHttpRequestException badRequest => (badRequest.StatusCode, "Bad Request", badRequest.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.UniqueViolation } } =>
                (StatusCodes.Status409Conflict, "Conflict", "A resource with the same unique value already exists."),
            DbUpdateException { InnerException: PostgresException { SqlState: PostgresErrorCodes.ForeignKeyViolation } } =>
                (StatusCodes.Status409Conflict, "Conflict", "The resource is in use and cannot be modified or deleted."),
            _ => (StatusCodes.Status500InternalServerError, "Internal Server Error", "An unexpected error occurred."),
        };

        if (status >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(
                exception,
                "Unhandled exception while processing {Method} {Path}. Correlation ID: {CorrelationId}",
                context.Request.Method,
                context.Request.Path,
                context.TraceIdentifier);
        }

        var problem = new ProblemDetails
        {
            Type = $"https://tools.ietf.org/html/rfc9110#section-{Rfc9110Section(status)}",
            Title = title,
            Status = status,
            Detail = detail,
            Instance = context.Request.Path,
        };
        problem.Extensions["correlationId"] = context.TraceIdentifier;

        context.Response.StatusCode = status;
        context.Response.ContentType = "application/problem+json";
        await JsonSerializer.SerializeAsync(context.Response.Body, problem, SerializerOptions);
    }

    private static string Rfc9110Section(int status) => status switch
    {
        StatusCodes.Status400BadRequest => "15.5.1",
        StatusCodes.Status404NotFound => "15.5.5",
        StatusCodes.Status409Conflict => "15.5.10",
        _ => "15.6.1",
    };
}
