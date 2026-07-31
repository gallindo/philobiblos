using Philobiblos.Application.Common;

namespace Philobiblos.Application.Users.Commands;

public sealed record SignOutUserCommand;

public sealed class SignOutUserCommandHandler : ICommandHandler<SignOutUserCommand, Unit>
{
    public Task<Unit> Handle(SignOutUserCommand command, CancellationToken cancellationToken)
    {
        return Task.FromResult(Unit.Value);
    }
}
