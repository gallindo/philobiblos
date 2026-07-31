using FluentAssertions;
using Philobiblos.Application.Books.Commands;

namespace Philobiblos.UnitTests.Validators;

public sealed class BookValidatorTests
{
    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void CreateBookCommandValidator_rejects_missing_or_empty_title(string? title)
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand(title!, Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Title");
    }

    [Fact]
    public void CreateBookCommandValidator_rejects_title_longer_than_200_characters()
    {
        var validator = new CreateBookCommandValidator();
        var title = new string('x', 201);

        var result = validator.Validate(new CreateBookCommand(title, Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Title");
    }

    [Fact]
    public void CreateBookCommandValidator_accepts_title_of_exactly_200_characters()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand(new string('x', 200), Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateBookCommandValidator_rejects_empty_author_id()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.Empty, Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "AuthorId");
    }

    [Fact]
    public void CreateBookCommandValidator_rejects_empty_genre_id()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.Empty, null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "GenreId");
    }

    [Theory]
    [InlineData("0123456789")]
    [InlineData("9780306406157")]
    public void CreateBookCommandValidator_accepts_valid_isbn(string isbn)
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), isbn, null));

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("0123456788")]
    [InlineData("9780306406150")]
    [InlineData("123456789")]
    [InlineData("not-an-isbn")]
    [InlineData("01234567890")]
    public void CreateBookCommandValidator_rejects_invalid_isbn(string isbn)
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), isbn, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Isbn");
    }

    [Fact]
    public void CreateBookCommandValidator_accepts_missing_isbn()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateBookCommandValidator_rejects_year_before_1450()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), null, 1449));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "PublishedYear");
    }

    [Fact]
    public void CreateBookCommandValidator_rejects_year_after_current_year()
    {
        var validator = new CreateBookCommandValidator();
        var futureYear = DateTime.UtcNow.Year + 1;

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), null, futureYear));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "PublishedYear");
    }

    [Theory]
    [InlineData(1450)]
    [InlineData(2020)]
    public void CreateBookCommandValidator_accepts_year_within_bounds(int year)
    {
        var validator = new CreateBookCommandValidator();
        var upperBound = DateTime.UtcNow.Year;
        if (year > upperBound)
        {
            year = upperBound;
        }

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), null, year));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void CreateBookCommandValidator_accepts_null_year()
    {
        var validator = new CreateBookCommandValidator();

        var result = validator.Validate(new CreateBookCommand("Title", Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void UpdateBookCommandValidator_rejects_empty_title()
    {
        var validator = new UpdateBookCommandValidator();

        var result = validator.Validate(new UpdateBookCommand(Guid.NewGuid(), "   ", Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Title");
    }

    [Fact]
    public void UpdateBookCommandValidator_rejects_title_longer_than_200_characters()
    {
        var validator = new UpdateBookCommandValidator();

        var result = validator.Validate(new UpdateBookCommand(Guid.NewGuid(), new string('x', 201), Guid.NewGuid(), Guid.NewGuid(), null, null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Title");
    }

    [Fact]
    public void UpdateBookCommandValidator_rejects_invalid_isbn()
    {
        var validator = new UpdateBookCommandValidator();

        var result = validator.Validate(new UpdateBookCommand(Guid.NewGuid(), "Title", Guid.NewGuid(), Guid.NewGuid(), "bad-isbn", null));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Isbn");
    }

    [Fact]
    public void UpdateBookCommandValidator_rejects_year_out_of_bounds()
    {
        var validator = new UpdateBookCommandValidator();

        var result = validator.Validate(new UpdateBookCommand(Guid.NewGuid(), "Title", Guid.NewGuid(), Guid.NewGuid(), null, 1449));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "PublishedYear");
    }
}
