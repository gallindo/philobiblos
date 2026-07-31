using Philobiblos.Application.Users.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Security;

namespace Philobiblos.Application.Users.Queries;

public sealed record GetCurrentUserQuery;

public sealed class GetCurrentUserQueryHandler : IQueryHandler<GetCurrentUserQuery, UserDto?>
{
    private readonly ICurrentUser _currentUser;

    public GetCurrentUserQueryHandler(ICurrentUser currentUser)
    {
        _currentUser = currentUser;
    }

    public Task<UserDto?> Handle(GetCurrentUserQuery query, CancellationToken cancellationToken)
    {
        if (!_currentUser.IsAuthenticated || _currentUser.Id is null)
        {
            return Task.FromResult<UserDto?>(null);
        }

        return Task.FromResult<UserDto?>(new UserDto(
            _currentUser.Id.Value,
            _currentUser.Email ?? string.Empty,
            _currentUser.DisplayName,
            _currentUser.Roles));
    }
}
