using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Philobiblos.Domain.Security;

namespace Philobiblos.Infrastructure.Security;

public sealed class HttpContextCurrentUser : ICurrentUser
{
    private readonly ClaimsPrincipal? _user;

    public HttpContextCurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _user = httpContextAccessor.HttpContext?.User;
    }

    public bool IsAuthenticated => _user?.Identity?.IsAuthenticated ?? false;

    public Guid? Id =>
        _user?.FindFirst(ClaimTypes.NameIdentifier)?.Value is { } value &&
        Guid.TryParse(value, out var id)
            ? id
            : null;

    public string? Email => _user?.FindFirst(ClaimTypes.Email)?.Value;

    public string? DisplayName => _user?.FindFirst(ClaimTypes.Name)?.Value;

    public IReadOnlyList<string> Roles =>
        _user?.FindAll(ClaimTypes.Role).Select(claim => claim.Value).ToList() ?? [];

    public bool IsInRole(string role) => _user?.IsInRole(role) ?? false;
}
