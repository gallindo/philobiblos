using FluentValidation;
using Philobiblos.Application.Users.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Users.Commands;

public sealed record UpdateUserRolesCommand(Guid UserId, Role Role);

public sealed class UpdateUserRolesCommandValidator : AbstractValidator<UpdateUserRolesCommand>
{
    public UpdateUserRolesCommandValidator()
    {
        RuleFor(command => command.UserId)
            .NotEmpty()
            .WithMessage("UserId is required.");
    }
}

public sealed class UpdateUserRolesCommandHandler : ICommandHandler<UpdateUserRolesCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateUserRolesCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(UpdateUserRolesCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByIdAsync(command.UserId, cancellationToken)
            ?? throw new NotFoundException($"User '{command.UserId}' was not found.");

        user.Role = command.Role;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            RoleMapping.ToRoleClaims(user.Role));
    }
}
