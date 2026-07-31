using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Common;
using Philobiblos.Application.Genres.Commands;
using Philobiblos.Application.Genres.Dtos;
using Philobiblos.Application.Genres.Queries;
using Philobiblos.Infrastructure.Filters;

namespace Philobiblos.Api.Endpoints;

public static class GenreEndpoints
{
    public static IEndpointRouteBuilder MapGenreEndpoints(this IEndpointRouteBuilder app)
    {
        var genres = app.MapGroup("/api/genres").WithTags("Genres");

        genres.MapPost("/", async (
            CreateGenreCommand command,
            ICommandHandler<CreateGenreCommand, GenreResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.Handle(command, cancellationToken);
            return TypedResults.CreatedAtRoute(response, "GetGenre", new { id = response.Id });
        })
            .AddEndpointFilter<ValidationFilter<CreateGenreCommand>>()
            .WithName("CreateGenre")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .RequireAuthorization("Editor");

        genres.MapGet("/", async (
            [AsParameters] ListGenresQuery query,
            IQueryHandler<ListGenresQuery, PagedResult<GenreResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(query, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<ListGenresQuery>>()
            .WithName("ListGenres")
            .ProducesValidationProblem();

        genres.MapGet("/{id}", async (
            Guid id,
            IQueryHandler<GetGenreQuery, GenreResponse> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(new GetGenreQuery(id), cancellationToken));
        })
            .WithName("GetGenre")
            .ProducesProblem(StatusCodes.Status404NotFound);

        genres.MapPut("/{id}", async (
            Guid id,
            UpdateGenreCommand command,
            ICommandHandler<UpdateGenreCommand, GenreResponse> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(command with { Id = id }, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<UpdateGenreCommand>>()
            .WithName("UpdateGenre")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .RequireAuthorization("Editor");

        genres.MapDelete("/{id}", async (
            Guid id,
            ICommandHandler<DeleteGenreCommand, Unit> handler,
            CancellationToken cancellationToken) =>
        {
            await handler.Handle(new DeleteGenreCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
            .WithName("DeleteGenre")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization("Editor");

        return app;
    }
}
