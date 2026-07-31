namespace Philobiblos.Domain.Entities;

public sealed class Author : IEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Bio { get; set; }
}
