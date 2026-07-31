namespace Philobiblos.Domain.Security;

public interface ICurrentUser
{
    bool IsAuthenticated { get; }

    Guid? Id { get; }

    string? Email { get; }

    string? DisplayName { get; }

    IReadOnlyList<string> Roles { get; }

    bool IsInRole(string role);
}
