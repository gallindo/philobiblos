using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Genres.Commands;

public sealed record DeleteGenreCommand(Guid Id);

public sealed class DeleteGenreCommandHandler : ICommandHandler<DeleteGenreCommand, Unit>
{
    private readonly IGenreRepository _genreRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGenreCommandHandler(IGenreRepository genreRepository, IUnitOfWork unitOfWork)
    {
        _genreRepository = genreRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteGenreCommand command, CancellationToken cancellationToken)
    {
        var genre = await _genreRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Genre '{command.Id}' was not found.");

        if (await _genreRepository.IsGenreInUseAsync(command.Id, cancellationToken))
        {
            throw new ConflictException($"Genre '{command.Id}' is in use by one or more books and cannot be deleted.");
        }

        _genreRepository.Remove(genre);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
