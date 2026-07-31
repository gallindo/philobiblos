using FluentAssertions;
using FluentValidation.Results;
using Philobiblos.Application.Genres.Commands;

namespace Philobiblos.UnitTests.Validators;

public sealed class GenreValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t")]
    public void CreateGenreCommandValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new CreateGenreCommandValidator();

        var result = validator.Validate(new CreateGenreCommand(name!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateGenreCommandValidator_accepts_valid_name()
    {
        var validator = new CreateGenreCommandValidator();

        var result = validator.Validate(new CreateGenreCommand("Science Fiction"));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateGenreCommandValidator_rejects_name_longer_than_100_characters()
    {
        var validator = new CreateGenreCommandValidator();
        var name = new string('x', 101);

        var result = validator.Validate(new CreateGenreCommand(name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void CreateGenreCommandValidator_accepts_name_of_exactly_100_characters()
    {
        var validator = new CreateGenreCommandValidator();
        var name = new string('x', 100);

        var result = validator.Validate(new CreateGenreCommand(name));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void UpdateGenreCommandValidator_rejects_missing_or_empty_name(string? name)
    {
        var validator = new UpdateGenreCommandValidator();

        var result = validator.Validate(new UpdateGenreCommand(Guid.NewGuid(), name!));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }

    [Fact]
    public void UpdateGenreCommandValidator_rejects_name_longer_than_100_characters()
    {
        var validator = new UpdateGenreCommandValidator();
        var name = new string('x', 101);

        var result = validator.Validate(new UpdateGenreCommand(Guid.NewGuid(), name));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Name");
    }
}
