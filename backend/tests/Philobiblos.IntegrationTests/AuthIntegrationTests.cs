using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Philobiblos.Application.Common;
using Philobiblos.Application.Users.Dtos;

namespace Philobiblos.IntegrationTests;

[Collection("LibraryApi")]
public sealed class AuthIntegrationTests : IAsyncLifetime
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private readonly LibraryApiFixture _fixture;
    private readonly HttpClient _client;

    public AuthIntegrationTests(LibraryApiFixture fixture)
    {
        _fixture = fixture;
        _client = fixture.Factory.CreateClient(new WebApplicationFactoryClientOptions { HandleCookies = true, AllowAutoRedirect = false });
    }

    public async Task InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task Me_returns_null_when_anonymous()
    {
        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await response.Content.ReadAsStringAsync();
        body.Should().Be("null");
    }

    [Fact]
    public async Task TestLogin_creates_user_and_returns_identity()
    {
        var response = await _client.PostAsync("/api/auth/test-login", null);

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
        user.DisplayName.Should().Be("Test User");
        user.Roles.Should().Contain("Editor");
    }

    [Fact]
    public async Task Me_returns_current_user_after_test_login()
    {
        await _client.PostAsync("/api/auth/test-login", null);

        var response = await _client.GetAsync("/api/auth/me");

        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>(JsonOptions);
        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
    }

    [Fact]
    public async Task Write_endpoint_returns_401_when_anonymous()
    {
        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "Fantasy" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Write_endpoint_returns_201_after_test_login()
    {
        await _client.PostAsync("/api/auth/test-login", null);

        var response = await _client.PostAsJsonAsync("/api/genres", new { name = "Fantasy" }, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task Logout_requires_authentication()
    {
        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
    }

    [Fact]
    public async Task Logout_clears_authentication()
    {
        await _client.PostAsync("/api/auth/test-login", null);

        var response = await _client.PostAsync("/api/auth/logout", null);

        response.StatusCode.Should().Be(HttpStatusCode.Redirect);
        response.Headers.Location.Should().Be("/");

        var me = await _client.GetAsync("/api/auth/me");
        me.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await me.Content.ReadAsStringAsync();
        body.Should().Be("null");
    }
}
