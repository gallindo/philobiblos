namespace Philobiblos.Domain.Entities;

public sealed class Book : IEntity
{
    public Guid Id { get; set; }

    public string Title { get; set; } = string.Empty;

    public string? Isbn { get; set; }

    public int? PublishedYear { get; set; }

    public Guid AuthorId { get; set; }

    public Author Author { get; set; } = null!;

    public Guid GenreId { get; set; }

    public Genre Genre { get; set; } = null!;
}
