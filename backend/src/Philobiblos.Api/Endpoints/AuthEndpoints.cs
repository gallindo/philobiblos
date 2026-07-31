using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Philobiblos.Application.Common;
using Philobiblos.Application.Users.Commands;
using Philobiblos.Application.Users.Dtos;
using Philobiblos.Application.Users.Queries;
using Philobiblos.Domain.Entities;
using Philobiblos.Infrastructure.Filters;
using Philobiblos.Infrastructure.Security;

namespace Philobiblos.Api.Endpoints;

public static class AuthEndpoints
{
    public static IEndpointRouteBuilder MapAuthEndpoints(this IEndpointRouteBuilder app)
    {
        var options = app.ServiceProvider.GetRequiredService<IOptions<AuthOptions>>().Value;
        var environment = app.ServiceProvider.GetRequiredService<IHostEnvironment>();
        var auth = app.MapGroup("/api/auth").WithTags("Auth");

        auth.MapGet("/login", () =>
        {
            if (!options.Google.Enabled)
            {
                return Results.Problem(
                    title: "OAuth Disabled",
                    detail: "Google OAuth is not enabled.",
                    statusCode: StatusCodes.Status503ServiceUnavailable);
            }

            return Results.Challenge(
                new AuthenticationProperties { RedirectUri = "/api/auth/callback" },
                new[] { options.Google.AuthenticationScheme });
        })
            .WithName("Login")
            .ProducesProblem(StatusCodes.Status503ServiceUnavailable);

        auth.MapGet("/callback", async (
            HttpContext context,
            ICommandHandler<GetOrCreateUserCommand, UserDto> handler,
            CancellationToken cancellationToken) =>
        {
            var result = await context.AuthenticateAsync(options.Google.AuthenticationScheme);
            if (!result.Succeeded || result.Principal?.Identity?.IsAuthenticated != true)
            {
                return Results.Redirect("/login?error=oauth_failed");
            }

            var principal = result.Principal;
            var providerSubject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? string.Empty;
            var email = principal.FindFirst(ClaimTypes.Email)?.Value ?? string.Empty;
            var displayName = principal.FindFirst(ClaimTypes.Name)?.Value;

            var role = IsSeedAdmin(email, options)
                ? Role.Admin
                : Role.User;

            var user = await handler.Handle(
                new GetOrCreateUserCommand(
                    new ExternalUserInfo("Google", providerSubject, email, displayName),
                    role),
                cancellationToken);

            await SignInUserAsync(context, user);

            return Results.Redirect("/");
        })
            .WithName("AuthCallback")
            .ExcludeFromDescription();

        if (!environment.IsProduction() && options.Test.Enabled)
        {
            auth.MapPost("/test-login", async (
                HttpContext context,
                ICommandHandler<GetOrCreateUserCommand, UserDto> handler,
                CancellationToken cancellationToken) =>
            {
                var testRole = options.Test.Roles
                    .Select(r => Enum.TryParse<Role>(r, true, out var parsed) ? parsed : (Role?)null)
                    .FirstOrDefault(r => r.HasValue) ?? Role.User;

                var user = await handler.Handle(
                    new GetOrCreateUserCommand(
                        new ExternalUserInfo("Test", "test", options.Test.Email, options.Test.DisplayName),
                        testRole),
                    cancellationToken);

                var identity = new ClaimsIdentity(
                    new[]
                    {
                        new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                        new Claim(ClaimTypes.Email, user.Email),
                        new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
                    }.Concat(options.Test.Roles.Select(role => new Claim(ClaimTypes.Role, role)))
                    .ToArray(),
                    CookieAuthenticationDefaults.AuthenticationScheme);

                await context.SignInAsync(
                    CookieAuthenticationDefaults.AuthenticationScheme,
                    new ClaimsPrincipal(identity),
                    new AuthenticationProperties { IsPersistent = true });

                return Results.Ok(user);
            })
                .WithName("TestLogin")
                .Produces<UserDto>(StatusCodes.Status200OK);
        }

        auth.MapPost("/logout", () =>
        {
            return Results.SignOut(
                new AuthenticationProperties { RedirectUri = "/" },
                new[] { CookieAuthenticationDefaults.AuthenticationScheme });
        })
            .WithName("Logout")
            .RequireAuthorization();

        auth.MapGet("/me", async (
            IQueryHandler<GetCurrentUserQuery, UserDto?> handler,
            CancellationToken cancellationToken) =>
        {
            var user = await handler.Handle(new GetCurrentUserQuery(), cancellationToken);
            return user is null
                ? Results.Content("null", "application/json")
                : Results.Json(user);
        })
            .WithName("GetCurrentUser")
            .Produces<UserDto?>(StatusCodes.Status200OK);

        auth.MapPatch("/users/{id:guid}/roles", async (
            Guid id,
            UpdateUserRolesCommand command,
            ICommandHandler<UpdateUserRolesCommand, UserDto> handler,
            CancellationToken cancellationToken) =>
        {
            return Results.Ok(await handler.Handle(command with { UserId = id }, cancellationToken));
        })
            .AddEndpointFilter<ValidationFilter<UpdateUserRolesCommand>>()
            .WithName("UpdateUserRoles")
            .RequireAuthorization("Admin")
            .ProducesProblem(StatusCodes.Status403Forbidden)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesValidationProblem();

        return app;
    }

    private static bool IsSeedAdmin(string email, AuthOptions options)
    {
        return !string.IsNullOrWhiteSpace(options.SeedAdminEmail) &&
               string.Equals(options.SeedAdminEmail, email, StringComparison.OrdinalIgnoreCase);
    }

    private static async Task SignInUserAsync(HttpContext context, UserDto user)
    {
        var identity = new ClaimsIdentity(
            new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Name, user.DisplayName ?? user.Email),
            }.Concat(user.Roles.Select(role => new Claim(ClaimTypes.Role, role)))
            .ToArray(),
            CookieAuthenticationDefaults.AuthenticationScheme);

        await context.SignInAsync(
            CookieAuthenticationDefaults.AuthenticationScheme,
            new ClaimsPrincipal(identity),
            new AuthenticationProperties { IsPersistent = true });
    }
}
