using FluentAssertions;
using Philobiblos.Application.Authors.Commands;

namespace Philobiblos.UnitTests.Validators;

public sealed class AuthorValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateAuthorCommandValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new CreateAuthorCommandValidator();

        var result = validator.Validate(new CreateAuthorCommand(name!, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateAuthorCommandValidator_rejects_name_longer_than_150_characters()
    {
        var validator = new CreateAuthorCommandValidator();
        var name = new string('x', 151);

        var result = validator.Validate(new CreateAuthorCommand(name, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateAuthorCommandValidator_accepts_name_of_exactly_150_characters()
    {
        var validator = new CreateAuthorCommandValidator();
        var name = new string('x', 150);

        var result = validator.Validate(new CreateAuthorCommand(name, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAuthorCommandValidator_rejects_bio_longer_than_2000_characters()
    {
        var validator = new CreateAuthorCommandValidator();
        var bio = new string('x', 2001);

        var result = validator.Validate(new CreateAuthorCommand("Author Name", bio));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Bio");
    }

    [Fact]
    public void CreateAuthorCommandValidator_accepts_bio_of_exactly_2000_characters()
    {
        var validator = new CreateAuthorCommandValidator();
        var bio = new string('x', 2000);

        var result = validator.Validate(new CreateAuthorCommand("Author Name", bio));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAuthorCommandValidator_accepts_null_bio()
    {
        var validator = new CreateAuthorCommandValidator();

        var result = validator.Validate(new CreateAuthorCommand("Author Name", null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdateAuthorCommandValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new UpdateAuthorCommandValidator();

        var result = validator.Validate(new UpdateAuthorCommand(Guid.NewGuid(), name!, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateAuthorCommandValidator_rejects_name_longer_than_150_characters()
    {
        var validator = new UpdateAuthorCommandValidator();
        var name = new string('x', 151);

        var result = validator.Validate(new UpdateAuthorCommand(Guid.NewGuid(), name, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateAuthorCommandValidator_rejects_bio_longer_than_2000_characters()
    {
        var validator = new UpdateAuthorCommandValidator();

        var result = validator.Validate(new UpdateAuthorCommand(Guid.NewGuid(), "Author Name", new string('x', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Bio");
    }
}
