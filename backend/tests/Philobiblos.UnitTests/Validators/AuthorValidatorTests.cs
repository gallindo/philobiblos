using FluentAssertions;
using Philobiblos.Api.Features.Authors;

namespace Philobiblos.UnitTests.Validators;

public sealed class AuthorValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateAuthorValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new CreateAuthorValidator();

        var result = validator.Validate(new CreateAuthorRequest(name!, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateAuthorValidator_rejects_name_longer_than_150_characters()
    {
        var validator = new CreateAuthorValidator();
        var name = new string('x', 151);

        var result = validator.Validate(new CreateAuthorRequest(name, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateAuthorValidator_accepts_name_of_exactly_150_characters()
    {
        var validator = new CreateAuthorValidator();
        var name = new string('x', 150);

        var result = validator.Validate(new CreateAuthorRequest(name, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAuthorValidator_rejects_bio_longer_than_2000_characters()
    {
        var validator = new CreateAuthorValidator();
        var bio = new string('x', 2001);

        var result = validator.Validate(new CreateAuthorRequest("Author Name", bio));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Bio");
    }

    [Fact]
    public void CreateAuthorValidator_accepts_bio_of_exactly_2000_characters()
    {
        var validator = new CreateAuthorValidator();
        var bio = new string('x', 2000);

        var result = validator.Validate(new CreateAuthorRequest("Author Name", bio));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateAuthorValidator_accepts_null_bio()
    {
        var validator = new CreateAuthorValidator();

        var result = validator.Validate(new CreateAuthorRequest("Author Name", null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void UpdateAuthorValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new UpdateAuthorValidator();

        var result = validator.Validate(new UpdateAuthorRequest(name!, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateAuthorValidator_rejects_name_longer_than_150_characters()
    {
        var validator = new UpdateAuthorValidator();
        var name = new string('x', 151);

        var result = validator.Validate(new UpdateAuthorRequest(name, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateAuthorValidator_rejects_bio_longer_than_2000_characters()
    {
        var validator = new UpdateAuthorValidator();

        var result = validator.Validate(new UpdateAuthorRequest("Author Name", new string('x', 2001)));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Bio");
    }
}
