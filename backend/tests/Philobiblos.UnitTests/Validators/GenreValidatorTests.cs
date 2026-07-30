using FluentAssertions;
using FluentValidation.Results;
using Philobiblos.Api.Features.Genres;

namespace Philobiblos.UnitTests.Validators;

public sealed class GenreValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateGenreValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new CreateGenreValidator();

        var result = validator.Validate(new CreateGenreRequest(name!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateGenreValidator_accepts_valid_name()
    {
        var validator = new CreateGenreValidator();

        var result = validator.Validate(new CreateGenreRequest("Science Fiction"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateGenreValidator_rejects_name_longer_than_100_characters()
    {
        var validator = new CreateGenreValidator();
        var name = new string('x', 101);

        var result = validator.Validate(new CreateGenreRequest(name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateGenreValidator_accepts_name_of_exactly_100_characters()
    {
        var validator = new CreateGenreValidator();
        var name = new string('x', 100);

        var result = validator.Validate(new CreateGenreRequest(name));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateGenreValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new UpdateGenreValidator();

        var result = validator.Validate(new UpdateGenreRequest(name!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateGenreValidator_rejects_name_longer_than_100_characters()
    {
        var validator = new UpdateGenreValidator();
        var name = new string('x', 101);

        var result = validator.Validate(new UpdateGenreRequest(name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }
}
