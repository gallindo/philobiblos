using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Authors.Commands;
using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Authors.Queries;
using Philobiblos.Application.Common;
using Philobiblos.Infrastructure.Filters;

namespace Philobiblos.Api.Endpoints;

public static class AuthorEndpoints
{
    public static IEndpointRouteBuilder MapAuthorEndpoints(this IEndpointRouteBuilder app)
    {
        var authors = app.MapGroup("/api/authors").WithTags("Authors");

        authors.MapPost("/", async (
            CreateAuthorCommand command,
            ICommandHandler<CreateAuthorCommand, AuthorResponse> handler,
            CancellationToken cancellationToken) =>
        {
            var response = await handler.Handle(command, cancellationToken);
            return TypedResults.CreatedAtRoute(response, "GetAuthor", new { id = response.Id });
        })
            .AddEndpointFilter<ValidationFilter<CreateAuthorCommand>>()
            .WithName("CreateAuthor")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .RequireAuthorization("Editor");

        authors.MapGet("/", async (
            [AsParameters] ListAuthorsQuery query,
            IQueryHandler<ListAuthorsQuery, PagedResult<AuthorResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(query, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<ListAuthorsQuery>>()
            .WithName("ListAuthors")
            .ProducesValidationProblem();

        authors.MapGet("/{id}", async (
            Guid id,
            IQueryHandler<GetAuthorQuery, AuthorResponse> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(new GetAuthorQuery(id), cancellationToken));
        })
            .WithName("GetAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound);

        authors.MapPut("/{id}", async (
            Guid id,
            UpdateAuthorCommand command,
            ICommandHandler<UpdateAuthorCommand, AuthorResponse> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(command with { Id = id }, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<UpdateAuthorCommand>>()
            .WithName("UpdateAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem()
            .RequireAuthorization("Editor");

        authors.MapDelete("/{id}", async (
            Guid id,
            ICommandHandler<DeleteAuthorCommand, Unit> handler,
            CancellationToken cancellationToken) =>
        {
            await handler.Handle(new DeleteAuthorCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
            .WithName("DeleteAuthor")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .RequireAuthorization("Editor");

        return app;
    }
}
