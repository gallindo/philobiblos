using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;

namespace Philobiblos.Infrastructure.Security;

public sealed class AuthOptions
{
    public const string SectionName = "Auth";

    public string SeedAdminEmail { get; set; } = string.Empty;

    public GoogleAuthOptions Google { get; set; } = new();

    public CookieAuthOptions Cookie { get; set; } = new();

    public TestAuthOptions Test { get; set; } = new();

    public DefaultAdminAuthOptions DefaultAdmin { get; set; } = new();
}

public sealed class TestAuthOptions
{
    public bool Enabled { get; set; } = false;

    public string Email { get; set; } = "test@example.com";

    public string DisplayName { get; set; } = "Test User";

    public List<string> Roles { get; set; } = ["Editor"];
}

public sealed class GoogleAuthOptions
{
    public bool Enabled { get; set; } = false;

    public string AuthenticationScheme { get; set; } = "Google";

    [Required]
    public string ClientId { get; set; } = string.Empty;

    [Required]
    public string ClientSecret { get; set; } = string.Empty;
}

public sealed class CookieAuthOptions
{
    public string LoginPath { get; set; } = "/api/auth/login";

    public string LogoutPath { get; set; } = "/api/auth/logout";

    public TimeSpan ExpireTimeSpan { get; set; } = TimeSpan.FromDays(14);

    public bool SlidingExpiration { get; set; } = true;
}

public sealed class DefaultAdminAuthOptions
{
    public bool Enabled { get; set; } = false;

    public string Email { get; set; } = string.Empty;

    public string Password { get; set; } = string.Empty;

    public string Roles { get; set; } = "Admin,Editor";
}

public sealed class AuthOptionsValidator : IValidateOptions<AuthOptions>
{
    public ValidateOptionsResult Validate(string? name, AuthOptions options)
    {
        var failures = new List<string>();

        if (options.Google.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.Google.ClientId))
            {
                failures.Add("Auth:Google:ClientId is required when Google OAuth is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.Google.ClientSecret))
            {
                failures.Add("Auth:Google:ClientSecret is required when Google OAuth is enabled.");
            }
        }

        if (options.DefaultAdmin.Enabled)
        {
            if (string.IsNullOrWhiteSpace(options.DefaultAdmin.Email))
            {
                failures.Add("Auth:DefaultAdmin:Email is required when the default admin seed is enabled.");
            }

            if (string.IsNullOrWhiteSpace(options.DefaultAdmin.Password))
            {
                failures.Add("Auth:DefaultAdmin:Password is required when the default admin seed is enabled.");
            }
        }

        return failures.Count == 0
            ? ValidateOptionsResult.Success
            : ValidateOptionsResult.Fail(failures);
    }
}
