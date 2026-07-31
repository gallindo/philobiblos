using FluentValidation;
using Philobiblos.Application.Common;
using Philobiblos.Application.Genres.Dtos;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Genres.Commands;

public sealed record UpdateGenreCommand(Guid Id, string Name);

public sealed class UpdateGenreCommandValidator : AbstractValidator<UpdateGenreCommand>
{
    public UpdateGenreCommandValidator()
    {
        RuleFor(command => command.Name)
            .Cascade(CascadeMode.Stop)
            .Must(name => !string.IsNullOrWhiteSpace(name))
            .WithMessage("Name is required.")
            .Must(name => (name ?? string.Empty).Trim().Length <= 100)
            .WithMessage("Name must be 100 characters or fewer.");
    }
}

public sealed class UpdateGenreCommandHandler : ICommandHandler<UpdateGenreCommand, GenreResponse>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public UpdateGenreCommandHandler(IGenreRepository genreRepository, IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GenreResponse> Handle(UpdateGenreCommand command, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{command.Id}' was not found.");

        var name = command.Name.Trim();

        if (await _genreRepository.IsNameTakenAsync(name, command.Id, cancellationToken))
        {
            throw new ConflictException($"A genre named '{name}' already exists.");
        }

        genre.Name = name;
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return GenreMapping.ToResponse(genre);
    }
}
