using FluentAssertions;
using Philobiblos.Application.Books;

namespace Philobiblos.UnitTests.Validators;

public sealed class IsbnValidatorTests
{
    [Theory]
    [InlineData("0-306-40615-2", "0306406152")]
    [InlineData("978-0-306-40615-7", "9780306406157")]
    [InlineData("  978-0-306-40615-7  ", "9780306406157")]
    public void Normalize_removes_hyphens_and_spaces(string input, string expected)
    {
        IsbnValidator.Normalize(input).Should().Be(expected);
    }

    [Theory]
    [InlineData("0306406152")]
    [InlineData("0-306-40615-2")]
    [InlineData("9780306406157")]
    [InlineData("978-0-306-40615-7")]
    [InlineData(" 978-0-306-40615-7 ")]
    public void IsValid_accepts_valid_isbn(string isbn)
    {
        IsbnValidator.IsValid(isbn).Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("123456789")]
    [InlineData("12345678901")]
    [InlineData("12345678901234")]
    [InlineData("not-an-isbn")]
    [InlineData("0123456788")]
    [InlineData("9780306406150")]
    [InlineData("030640615X")]
    public void IsValid_rejects_invalid_isbn(string? isbn)
    {
        IsbnValidator.IsValid(isbn!).Should().BeFalse();
    }
}
