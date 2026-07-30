namespace Philobiblos.Api.Domain;

public sealed class Author : IEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string? Bio { get; set; }
}
