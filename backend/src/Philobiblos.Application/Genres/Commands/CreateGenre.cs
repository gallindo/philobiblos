using FluentValidation;
using Philobiblos.Application.Common;
using Philobiblos.Application.Genres.Dtos;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Genres.Commands;

public sealed record CreateGenreCommand(string Name);

public sealed class CreateGenreCommandValidator : AbstractValidator<CreateGenreCommand>
{
    public CreateGenreCommandValidator()
    {
        RuleFor(command => command.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 100)
            .WithMessage("Name must be 100 characters or fewer.");
    }
}

public sealed class CreateGenreCommandHandler : ICommandHandler<CreateGenreCommand, GenreResponse>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public CreateGenreCommandHandler(IGenreRepository genreRepository, IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GenreResponse> Handle(CreateGenreCommand command, CancellationToken cancellationToken)
    {
        var name = command.Name.Trim();

        if (await _genreRepository.IsNameTakenAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A genre named '{name}' already exists.");
        }

        var genre = new Genre { Id = Guid.CreateVersion7(), Name = name };
        _genreRepository.Add(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GenreMapping.ToResponse(genre);
    }
}
