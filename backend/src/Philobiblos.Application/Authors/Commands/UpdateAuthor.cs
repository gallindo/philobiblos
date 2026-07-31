using FluentValidation;
using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Authors.Commands;

public sealed record UpdateAuthorCommand(Guid Id, string Name, string? Bio);

public sealed class UpdateAuthorCommandValidator : AbstractValidator<UpdateAuthorCommand>
{
    public UpdateAuthorCommandValidator()
    {
        RuleFor(command => command.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 150)
            .WithMessage("Name must be 150 characters or fewer.");

        RuleFor(command => command.Bio)
            .MaximumLength(2000)
            .WithMessage("Bio must be 2000 characters or fewer.");
    }
}

public sealed class UpdateAuthorCommandHandler : ICommandHandler<UpdateAuthorCommand, AuthorResponse>
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateAuthorCommandHandler(IAuthorRepository authorRepository, IUnitOfWork unitOfWork)
    {
        _authorRepository = authorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthorResponse> Handle(UpdateAuthorCommand command, CancellationToken cancellationToken)
    {
        var author = await _authorRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Author '{command.Id}' was not found.");

        var name = command.Name.Trim();

        if (await _authorRepository.IsNameTakenAsync(name, command.Id, cancellationToken))
        {
            throw new ConflictException($"An author named '{name}' already exists.");
        }

        author.Name = name;
        author.Bio = command.Bio;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthorMapping.ToResponse(author);
    }
}
