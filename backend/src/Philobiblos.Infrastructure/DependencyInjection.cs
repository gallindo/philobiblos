using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Repositories;
using Philobiblos.Domain.Security;
using Philobiblos.Infrastructure.Data;
using Philobiblos.Infrastructure.HostedServices;
using Philobiblos.Infrastructure.Repositories;
using Philobiblos.Infrastructure.Security;

namespace Philobiblos.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<LibraryDbContext>(options =>
            options.UseNpgsql(configuration.GetConnectionString("Library")));

        services.AddScoped<IUnitOfWork>(provider => provider.GetRequiredService<LibraryDbContext>());
        services.AddScoped<IAuthorRepository, AuthorRepository>();
        services.AddScoped<IBookRepository, BookRepository>();
        services.AddScoped<IGenreRepository, GenreRepository>();
        services.AddScoped<IUserRepository, UserRepository>();

        services.AddAuthenticationAndAuthorization(configuration);

        services.AddSingleton<PasswordHasher<User>>();
        services.AddHostedService<DefaultAdminSeeder>();

        return services;
    }

    public static IServiceCollection AddAuthenticationAndAuthorization(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddSingleton<IValidateOptions<AuthOptions>, AuthOptionsValidator>();
        services.AddOptions<AuthOptions>()
            .BindConfiguration(AuthOptions.SectionName)
            .ValidateOnStart();

        var authOptions = configuration.GetSection(AuthOptions.SectionName).Get<AuthOptions>() ?? new AuthOptions();

        services.AddAuthentication(options =>
        {
            options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
            options.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        })
        .AddCookie(options =>
        {
            options.LoginPath = authOptions.Cookie.LoginPath;
            options.LogoutPath = authOptions.Cookie.LogoutPath;
            options.ExpireTimeSpan = authOptions.Cookie.ExpireTimeSpan;
            options.SlidingExpiration = authOptions.Cookie.SlidingExpiration;
            options.Cookie.HttpOnly = true;
            options.Cookie.SameSite = SameSiteMode.Lax;
            options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
            options.Events.OnRedirectToLogin = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
            options.Events.OnRedirectToAccessDenied = context =>
            {
                if (context.Request.Path.StartsWithSegments("/api"))
                {
                    context.Response.StatusCode = StatusCodes.Status403Forbidden;
                    return Task.CompletedTask;
                }

                context.Response.Redirect(context.RedirectUri);
                return Task.CompletedTask;
            };
        });

        if (authOptions.Google.Enabled)
        {
            services.AddAuthentication()
                .AddGoogle(authOptions.Google.AuthenticationScheme, options =>
                {
                    options.ClientId = authOptions.Google.ClientId;
                    options.ClientSecret = authOptions.Google.ClientSecret;
                    options.CallbackPath = "/api/auth/callback";
                    options.Scope.Add("openid");
                    options.Scope.Add("email");
                    options.Scope.Add("profile");
                });
        }

        services.AddAuthorization(options =>
        {
            options.AddPolicy("Editor", policy =>
                policy.RequireAuthenticatedUser().RequireRole("Editor", "Admin"));

            options.AddPolicy("Admin", policy =>
                policy.RequireAuthenticatedUser().RequireRole("Admin"));
        });

        services.AddHttpContextAccessor();
        services.AddScoped<ICurrentUser, HttpContextCurrentUser>();

        return services;
    }
}
