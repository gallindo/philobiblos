using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Philobiblos.Application.Common;
using System.Reflection;

namespace Philobiblos.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddValidatorsFromAssemblyContaining<ApplicationAssemblyMarker>();

        var assembly = typeof(ApplicationAssemblyMarker).Assembly;

        foreach (var type in assembly.GetTypes().Where(t => t.IsClass && !t.IsAbstract))
        {
            foreach (var iface in type.GetInterfaces())
            {
                if (iface.IsGenericType &&
                    (iface.GetGenericTypeDefinition() == typeof(ICommandHandler<,>) ||
                     iface.GetGenericTypeDefinition() == typeof(IQueryHandler<,>)))
                {
                    services.AddTransient(iface, type);
                }
            }
        }

        return services;
    }
}
