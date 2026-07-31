using FluentAssertions;
using Philobiblos.Application.Users;
using Philobiblos.Application.Users.Commands;
using Philobiblos.Application.Users.Queries;
using Philobiblos.Domain.Entities;
using Philobiblos.Domain.Exceptions;
using Philobiblos.Domain.Security;
using Philobiblos.UnitTests.Common;

namespace Philobiblos.UnitTests.Handlers;

public sealed class UserBusinessRuleTests
{
    [Theory]
    [InlineData(Role.User, 0)]
    [InlineData(Role.Editor, 1)]
    [InlineData(Role.Admin, 2)]
    public void RoleMapping_returns_expected_claims(Role role, int expectedCount)
    {
        var claims = RoleMapping.ToRoleClaims(role);

        claims.Should().HaveCount(expectedCount);
    }

    [Fact]
    public async Task GetOrCreateUser_creates_new_user_when_none_exists()
    {
        await using var harness = new TestHarness();
        var handler = new GetOrCreateUserCommandHandler(harness.Users, harness.UnitOfWork);

        var user = await handler.Handle(
            new GetOrCreateUserCommand(
                new ExternalUserInfo("Google", "google-123", "user@example.com", "User"),
                Role.Editor),
            default);

        user.Email.Should().Be("user@example.com");
        user.DisplayName.Should().Be("User");
        user.Roles.Should().Contain("Editor");
    }

    [Fact]
    public async Task GetOrCreateUser_updates_existing_user_instead_of_creating_duplicate()
    {
        await using var harness = new TestHarness();
        var handler = new GetOrCreateUserCommandHandler(harness.Users, harness.UnitOfWork);
        var first = await handler.Handle(
            new GetOrCreateUserCommand(
                new ExternalUserInfo("Google", "google-123", "old@example.com", "Old"),
                Role.User),
            default);

        var second = await handler.Handle(
            new GetOrCreateUserCommand(
                new ExternalUserInfo("Google", "google-123", "new@example.com", "New"),
                Role.User),
            default);

        second.Id.Should().Be(first.Id);
        second.Email.Should().Be("new@example.com");
        second.DisplayName.Should().Be("New");
    }

    [Fact]
    public async Task GetOrCreateUser_validation_rejects_invalid_email()
    {
        var validator = new GetOrCreateUserCommandValidator();
        var result = await validator.ValidateAsync(
            new GetOrCreateUserCommand(
                new ExternalUserInfo("Google", "sub", "not-an-email", null),
                Role.User));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateUserRoles_changes_user_role()
    {
        await using var harness = new TestHarness();
        var createHandler = new GetOrCreateUserCommandHandler(harness.Users, harness.UnitOfWork);
        var created = await createHandler.Handle(
            new GetOrCreateUserCommand(
                new ExternalUserInfo("Google", "google-123", "user@example.com", "User"),
                Role.User),
            default);

        var updateHandler = new UpdateUserRolesCommandHandler(harness.Users, harness.UnitOfWork);
        var updated = await updateHandler.Handle(
            new UpdateUserRolesCommand(created.Id, Role.Admin),
            default);

        updated.Roles.Should().Contain("Admin");
        updated.Roles.Should().Contain("Editor");
    }

    [Fact]
    public async Task UpdateUserRoles_throws_not_found_when_user_missing()
    {
        await using var harness = new TestHarness();
        var handler = new UpdateUserRolesCommandHandler(harness.Users, harness.UnitOfWork);

        var action = async () =>
            await handler.Handle(new UpdateUserRolesCommand(Guid.CreateVersion7(), Role.Editor), default);

        await action.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task GetCurrentUser_returns_null_when_anonymous()
    {
        var handler = new GetCurrentUserQueryHandler(new TestCurrentUser());

        var user = await handler.Handle(new GetCurrentUserQuery(), default);

        user.Should().BeNull();
    }

    [Fact]
    public async Task GetCurrentUser_returns_identity_when_authenticated()
    {
        var currentUser = new TestCurrentUser
        {
            IsAuthenticated = true,
            Id = Guid.CreateVersion7(),
            Email = "test@example.com",
            DisplayName = "Test",
            Roles = ["Editor"],
        };
        var handler = new GetCurrentUserQueryHandler(currentUser);

        var user = await handler.Handle(new GetCurrentUserQuery(), default);

        user.Should().NotBeNull();
        user!.Email.Should().Be("test@example.com");
        user.Roles.Should().Contain("Editor");
    }

    private sealed class TestCurrentUser : ICurrentUser
    {
        public bool IsAuthenticated { get; set; }
        public Guid? Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string? DisplayName { get; set; }
        public IReadOnlyList<string> Roles { get; set; } = [];

        public bool IsInRole(string role) => Roles.Contains(role);
    }
}
