using System.Text.RegularExpressions;
using FluentValidation;
using Microsoft.AspNetCore.Identity;
using Philobiblos.Application.Common;
using Philobiblos.Application.Users.Dtos;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Users.Commands;

public sealed record RegisterUserCommand(string Email, string Password, string? DisplayName = null);

public sealed class RegisterUserCommandValidator : AbstractValidator<RegisterUserCommand>
{
    private static readonly Regex PasswordPolicy = new(
        @"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$",
        RegexOptions.Compiled,
        TimeSpan.FromMilliseconds(100));

    public RegisterUserCommandValidator()
    {
        RuleFor(command => command.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email is required.");

        RuleFor(command => command.Password)
            .NotEmpty()
            .Must(BeStrongPassword)
            .WithMessage("Password must be at least 8 characters and contain uppercase, lowercase, digit, and special characters.");
    }

    private static bool BeStrongPassword(string password)
    {
        return PasswordPolicy.IsMatch(password);
    }
}

public sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly PasswordHasher<User> _passwordHasher;

    public RegisterUserCommandHandler(
        IUserRepository userRepository,
        IUnitOfWork unitOfWork,
        PasswordHasher<User> passwordHasher)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
        _passwordHasher = passwordHasher;
    }

    public async Task<UserDto> Handle(RegisterUserCommand command, CancellationToken cancellationToken)
    {
        var normalizedEmail = command.Email.ToLowerInvariant();
        var existing = await _userRepository.GetByEmailAsync(normalizedEmail, cancellationToken);
        if (existing is not null)
        {
            throw new ConflictException("A user with this email already exists.");
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = normalizedEmail,
            DisplayName = command.DisplayName,
            Provider = "Local",
            ProviderSubject = Guid.CreateVersion7().ToString(),
            PasswordHash = _passwordHasher.HashPassword(null!, command.Password),
            Role = Role.User,
            CreatedAt = DateTimeOffset.UtcNow,
            LastSignedInAt = null,
        };

        _userRepository.Add(user);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            RoleMapping.ToRoleClaims(user.Role));
    }
}
