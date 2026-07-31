using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Philobiblos.Application.Common;
using Philobiblos.Application.Users.Dtos;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Users.Commands;

public sealed record LoginUserCommand(string Email, string Password);

public sealed class LoginUserCommandValidator : AbstractValidator<LoginUserCommand>
{
    public LoginUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email is required.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .WithMessage("Password is required.");
    }
}

public sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, UserDto?>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher;

    public LoginUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        PasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto?> Handle(LoginUserCommand command, CancellationToken cancellationToken)
    {
        var user = await _userRepository.GetByEmailAsync(
            command.Email.ToLowerInvariant(),
            cancellationToken);

        if (user is null || user.PasswordHash is null)
        {
            return null;
        }

        var result = _passwordHasher.VerifyHashedPassword(null!, user.PasswordHash, command.Password);
        if (result == PasswordVerificationResult.Failed)
        {
            return null;
        }

        user.LastSignedInAt = DateTimeOffset.UtcNow;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            RoleMapping.ToRoleClaims(user.Role));
    }
}
