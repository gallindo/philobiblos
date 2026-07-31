using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Philobiblos.Application.Books.Commands;
using Philobiblos.Application.Books.Dtos;
using Philobiblos.Application.Books.Queries;
using Philobiblos.Application.Common;
using Philobiblos.Infrastructure.Filters;

namespace Philobiblos.Api.Endpoints;

public static class BookEndpoints
{
    public static IEndpointRouteBuilder MapBookEndpoints(this IEndpointRouteBuilder app)
    {
        var books = app.MapGroup("/api/books").WithTags("Books");

        books.MapPost("/", async Task<Results<ValidationProblem, CreatedAtRoute<BookResponse>>> (
            CreateBookCommand command,
            ICommandHandler<CreateBookCommand, Result<BookResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command, cancellationToken);
            if (!result.IsSuccess)
            {
                return TypedResults.ValidationProblem(
                    result.Errors,
                    detail: "One or more validation errors occurred.",
                    title: "Bad Request");
            }

            return TypedResults.CreatedAtRoute(result.Value, "GetBook", new { id = result.Value!.Id });
        })
            .AddEndpointFilter<ValidationFilter<CreateBookCommand>>()
            .WithName("CreateBook")
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        books.MapGet("/", async (
            [AsParameters] ListBooksQuery query,
            IQueryHandler<ListBooksQuery, PagedResult<BookResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(query, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<ListBooksQuery>>()
            .WithName("ListBooks")
            .ProducesValidationProblem();

        books.MapGet("/{id}", async (
            Guid id,
            IQueryHandler<GetBookQuery, BookResponse> handler,
            CancellationToken cancellationToken) =>
        {
            return TypedResults.Ok(await handler.Handle(new GetBookQuery(id), cancellationToken));
        })
            .WithName("GetBook")
            .ProducesProblem(StatusCodes.Status404NotFound);

        books.MapPut("/{id}", async Task<Results<ValidationProblem, Ok<BookResponse>>> (
            Guid id,
            UpdateBookCommand command,
            ICommandHandler<UpdateBookCommand, Result<BookResponse>> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await handler.Handle(command with { Id = id }, cancellationToken);
            if (!result.IsSuccess)
            {
                return TypedResults.ValidationProblem(
                    result.Errors,
                    detail: "One or more validation errors occurred.",
                    title: "Bad Request");
            }

            return TypedResults.Ok(result.Value);
        })
            .AddEndpointFilter<ValidationFilter<UpdateBookCommand>>()
            .WithName("UpdateBook")
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict)
            .ProducesValidationProblem();

        books.MapDelete("/{id}", async (
            Guid id,
            ICommandHandler<DeleteBookCommand, Unit> handler,
            CancellationToken cancellationToken) =>
        {
            await handler.Handle(new DeleteBookCommand(id), cancellationToken);
            return TypedResults.NoContent();
        })
            .WithName("DeleteBook")
            .ProducesProblem(StatusCodes.Status404NotFound);

        return app;
    }
}
