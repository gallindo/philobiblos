namespace Philobiblos.Application.Users.Dtos;

public sealed record UserDto(
    Guid Id,
    string Email,
    string? DisplayName,
    IReadOnlyList<string> Roles);
