using FluentValidation;
using Philobiblos.Application.Authors.Dtos;
using Philobiblos.Application.Common;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Authors.Commands;

public sealed record CreateAuthorCommand(string Name, string? Bio);

public sealed class CreateAuthorCommandValidator : AbstractValidator<CreateAuthorCommand>
{
    public CreateAuthorCommandValidator()
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

public sealed class CreateAuthorCommandHandler : ICommandHandler<CreateAuthorCommand, AuthorResponse>
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateAuthorCommandHandler(IAuthorRepository authorRepository, IUnitOfWork unitOfWork)
    {
        _authorRepository = authorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<AuthorResponse> Handle(CreateAuthorCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();

        if (await _authorRepository.IsNameTakenAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"An author named '{name}' already exists.");
        }

        var author = new Author { Id = Guid.CreateVersion7(), Name = name, Bio = command.Bio };
        _authorRepository.Add(author);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return AuthorMapping.ToResponse(author);
    }
}
