using Philobiblos.Application.Common;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Repositories;

namespace Philobiblos.Application.Books.Commands;

public sealed record DeleteBookCommand(Guid Id);

public sealed class DeleteBookCommandHandler : ICommandHandler<DeleteBookCommand, Unit>
{
    private readonly IBookRepository _bookRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteBookCommandHandler(IBookRepository bookRepository, IUnitOfWork unitOfWork)
    {
        _bookRepository = bookRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<Unit> Handle(DeleteBookCommand command, CancellationToken cancellationToken)
    {
        var book = await _bookRepository.GetByIdAsync(command.Id, cancellationToken)
            ?? throw new NotFoundException($"Book '{command.Id}' was not found.");

        _bookRepository.Remove(book);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return Unit.Value;
    }
}
