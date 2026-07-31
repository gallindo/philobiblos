using FluentAssertions;
using Philobiblos.Application.Users.Commands;

namespace Philobiblos.UnitTests.Validators;

public sealed class RegisterUserCommandValidatorTests
{
    [Fact]
    public void Validator_accepts_valid_email_and_strong_password()
    {
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand("user@example.com", "Strong1!"));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("not-an-email")]
    public void Validator_rejects_invalid_email(string? email)
    {
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand(email!, "Strong1!"));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Email");
    }

    [Theory]
    [InlineData("short1!")]
    [InlineData("nouppercase1!")]
    [InlineData("NOLOWERCASE1!")]
    [InlineData("NoDigits!aa")]
    [InlineData("NoSpecial1aa")]
    public void Validator_rejects_weak_passwords(string password)
    {
        var validator = new RegisterUserCommandValidator();

        var result = validator.Validate(new RegisterUserCommand("user@example.com", password));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Password");
    }
}
