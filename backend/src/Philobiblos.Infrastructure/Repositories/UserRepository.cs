using Microsoft.EntityFrameworkCore;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Data;

namespace Philobiblos.Infrastructure.Repositories;

public sealed class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(LibraryDbContext context)
        : base(context)
    {
    }

    public Task<User?> GetByProviderAsync(
        string provider,
        string providerSubject,
        CancellationToken cancellationToken = default)
    {
        return Context.Users
            .FirstOrDefaultAsync(
                user => user.Provider == provider && user.ProviderSubject == providerSubject,
                cancellationToken);
    }

    public Task<bool> AnyAsync(CancellationToken cancellationToken = default)
    {
        return Context.Users.AnyAsync(cancellationToken);
    }
}
