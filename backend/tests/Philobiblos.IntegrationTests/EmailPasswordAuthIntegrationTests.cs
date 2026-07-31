using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Philobiblos.Application.Users.Dtos;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class EmailPasswordAuthIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public EmailPasswordAuthIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { AllowAutoRedirect = false });
    }

    public Task InitializeAsync() => _fixture.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Register_creates_user_and_allows_login()
    {
        var email = "local@example.com";
        var password = "Strong1!";

        var registerResponse = await _client.PostAsJsonAsync("/api/auth/register", new { email, password }, JsonOptions);
        registerResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await registerResponse.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user.Should().NotBeNull();
        user!.Email.Should().Be(email.ToLowerInvariant());

        var loginResponse = await _client.PostAsJsonAsync("/api/auth/login", new { email, password }, JsonOptions);
        loginResponse.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task Register_rejects_duplicate_email()
    {
        var email = "duplicate@example.com";
        var password = "Strong1!";

        var first = await _client.PostAsJsonAsync("/api/auth/register", new { email, password }, JsonOptions);
        first.StatusCode.Should().Be(HttpStatusCode.OK);

        var second = await _client.PostAsJsonAsync("/api/auth/register", new { email, password }, JsonOptions);
        second.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task Register_rejects_weak_password()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/register", new { email = "weak@example.com", password = "weak" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Login_rejects_unknown_email()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "missing@example.com", password = "Strong1!" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_rejects_wrong_password()
    {
        var email = "wrongpass@example.com";
        var password = "Strong1!";

        var register = await _client.PostAsJsonAsync("/api/auth/register", new { email, password }, JsonOptions);
        register.StatusCode.Should().Be(HttpStatusCode.OK);

        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email, password = "WrongPass1!" }, JsonOptions);
        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Login_rejects_oauth_only_account()
    {
        var response = await _client.PostAsJsonAsync("/api/auth/login", new { email = "test@example.com", password = "Strong1!" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }
}
