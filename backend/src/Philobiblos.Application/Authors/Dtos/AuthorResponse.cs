using Philobiblos.Domain.Entities;

namespace Philobiblos.Application.Authors.Dtos;

public sealed record AuthorResponse(Guid Id, string Name, string? Bio);

public static class AuthorMapping
{
    public static AuthorResponse ToResponse(Author author) => new(author.Id, author.Name, author.Bio);
}
