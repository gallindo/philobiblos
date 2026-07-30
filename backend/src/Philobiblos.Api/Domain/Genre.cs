namespace Philobiblos.Api.Domain;

public sealed class Genre : IEntity
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;
}
