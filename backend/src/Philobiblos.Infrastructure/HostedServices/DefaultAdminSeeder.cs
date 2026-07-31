using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Infrastructure.Security;

namespace Philobiblos.Infrastructure.HostedServices;

public sealed class DefaultAdminSeeder : IHostedService
{
    private readonly IServiceProvider _serviceProvider;

    public DefaultAdminSeeder(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public async Task StartAsync(CancellationToken cancellationToken)
    {
        await using var scope = _serviceProvider.CreateAsyncScope();
        var options = scope.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;

        if (!options.DefaultAdmin.Enabled)
        {
            return;
        }

        var userRepository = scope.ServiceProvider.GetRequiredService<IUserRepository>();
        var passwordHasher = scope.ServiceProvider.GetRequiredService<PasswordHasher<User>>();
        var unitOfWork = scope.ServiceProvider.GetRequiredService<IUnitOfWork>();

        var existing = await userRepository.GetByEmailAsync(options.DefaultAdmin.Email, cancellationToken);
        if (existing is not null)
        {
            return;
        }

        var user = new User
        {
            Id = Guid.CreateVersion7(),
            Email = options.DefaultAdmin.Email.ToLowerInvariant(),
            DisplayName = "Default Administrator",
            Provider = "Local",
            ProviderSubject = Guid.CreateVersion7().ToString(),
            PasswordHash = passwordHasher.HashPassword(null!, options.DefaultAdmin.Password),
            Role = ResolveRole(options.DefaultAdmin.Roles),
            CreatedAt = DateTimeOffset.UtcNow,
            LastSignedInAt = null,
        };

        userRepository.Add(user);
        await unitOfWork.SaveChangesAsync(cancellationToken);
    }

    public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;

    private static Role ResolveRole(string roles)
    {
        var parsed = roles.Split(',', StringSplitOptions.RemoveEmptyEntries)
            .Select(role => role.Trim())
            .ToList();

        if (parsed.Contains("Admin", StringComparer.OrdinalIgnoreCase))
        {
            return Role.Admin;
        }

        if (parsed.Contains("Editor", StringComparer.OrdinalIgnoreCase))
        {
            return Role.Editor;
        }

        return Role.User;
    }
}
