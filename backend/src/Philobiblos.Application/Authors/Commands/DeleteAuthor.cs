using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Authors.Commands;

public sealed record DeleteAuthorCommand(Guid Id);

public sealed class DeleteAuthorCommandHandler : ICommandHandler<DeleteAuthorCommand, Unit>
{
    private readonly IAuthorRepository _authorRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteAuthorCommandHandler(IAuthorRepository authorRepository, IUnitOfWork unitOfWork)
    {
        _authorRepository = authorRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteAuthorCommand command, CancellationToken cancellationToken)
    {
        var author = await _authorRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Author '{command.Id}' was not found.");

        if (await _authorRepository.IsAuthorInUseAsync(command.Id, cancellationToken))
        {
            throw new ConflictException($"Author '{command.Id}' is in use by one or more books and cannot be deleted.");
        }

        _authorRepository.Remove(author);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
