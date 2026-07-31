using FluentValidation;
using Philobiblos.Application.Users.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Users.Commands;

public sealed record ExternalUserInfo(
    string Provider,
    string ProviderSubject,
    string Email,
    string? DisplayName);

public sealed record GetOrCreateUserCommand(ExternalUserInfo User, Role Role);

public sealed class GetOrCreateUserCommandValidator : AbstractValidator<GetOrCreateUserCommand>
{
    public GetOrCreateUserCommandValidator()
    {
        RuleFor(command => command.User.Provider)
            .NotEmpty()
            .WithMessage("Provider is required.");

        RuleFor(command => command.User.ProviderSubject)
            .NotEmpty()
            .WithMessage("ProviderSubject is required.");

        RuleFor(command => command.User.Email)
            .NotEmpty()
            .EmailAddress()
            .WithMessage("A valid email is required.");
    }
}

public sealed class GetOrCreateUserCommandHandler : ICommandHandler<GetOrCreateUserCommand, UserDto>
{
    private readonly IUserRepository _userRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GetOrCreateUserCommandHandler(IUserRepository userRepository, IUnitOfWork unitOfWork)
    {
        _userRepository = userRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<UserDto> Handle(GetOrCreateUserCommand command, CancellationToken cancellationToken)
    {
        var info = command.User;

        var user = await _userRepository.GetByProviderAsync(
            info.Provider,
            info.ProviderSubject,
            cancellationToken);

        if (user is null)
        {
            user = new User
            {
                Id = Guid.CreateVersion7(),
                Provider = info.Provider,
                ProviderSubject = info.ProviderSubject,
                Email = info.Email,
                DisplayName = info.DisplayName,
                Role = command.Role,
                CreatedAt = DateTimeOffset.UtcNow,
                LastSignedInAt = DateTimeOffset.UtcNow,
            };
            _userRepository.Add(user);
        }
        else
        {
            user.Email = info.Email;
            user.DisplayName = info.DisplayName;
            user.LastSignedInAt = DateTimeOffset.UtcNow;
        }

        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ToDto(user);
    }

    private static UserDto ToDto(User user)
    {
        return new UserDto(
            user.Id,
            user.Email,
            user.DisplayName,
            RoleMapping.ToRoleClaims(user.Role));
    }
}
