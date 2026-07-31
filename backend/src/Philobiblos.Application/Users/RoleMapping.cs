using Philobiblos.Domain.Entities;

namespace Philobiblos.Application.Users;

public static class RoleMapping
{
    public static IReadOnlyList<string> ToRoleClaims(Role role) => role switch
    {
        Role.Admin => ["Admin", "Editor"],
        Role.Editor => ["Editor"],
        _ => [],
    };
}
