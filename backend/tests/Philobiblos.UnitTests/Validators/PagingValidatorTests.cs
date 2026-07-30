using FluentAssertions;
using Philobiblos.Api.Features.Authors;
using Philobiblos.Api.Features.Books;
using Philobiblos.Api.Features.Genres;

namespace Philobiblos.UnitTests.Validators;

public sealed class PagingValidatorTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void PagedQueryValidator_rejects_page_less_than_one(int page)
    {
        var validator = new ListGenresQueryValidator();

        var result = validator.Validate(new ListGenresQuery { Page = page });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Page");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(101)]
    public void PagedQueryValidator_rejects_page_size_out_of_range(int pageSize)
    {
        var validator = new ListAuthorsQueryValidator();

        var result = validator.Validate(new ListAuthorsQuery { PageSize = pageSize });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "PageSize");
    }

    [Fact]
    public void PagedQueryValidator_accepts_page_and_page_size_at_bounds()
    {
        var validator = new ListBooksQueryValidator();

        var result = validator.Validate(new ListBooksQuery { Page = 1, PageSize = 100 });

        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("up")]
    [InlineData("ascending")]
    public void PagedQueryValidator_rejects_invalid_direction(string direction)
    {
        var validator = new ListGenresQueryValidator();

        var result = validator.Validate(new ListGenresQuery { Sort = "name", Direction = direction });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Direction");
    }

    [Fact]
    public void PagedQueryValidator_rejects_direction_without_sort()
    {
        var validator = new ListAuthorsQueryValidator();

        var result = validator.Validate(new ListAuthorsQuery { Direction = "desc" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Direction");
    }

    [Fact]
    public void ListGenresQueryValidator_accepts_supported_sort()
    {
        var validator = new ListGenresQueryValidator();

        var result = validator.Validate(new ListGenresQuery { Sort = "name", Direction = "desc" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListGenresQueryValidator_rejects_unsupported_sort()
    {
        var validator = new ListGenresQueryValidator();

        var result = validator.Validate(new ListGenresQuery { Sort = "id" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Sort");
    }

    [Fact]
    public void ListAuthorsQueryValidator_accepts_supported_sort()
    {
        var validator = new ListAuthorsQueryValidator();

        var result = validator.Validate(new ListAuthorsQuery { Sort = "name", Direction = "asc" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListAuthorsQueryValidator_rejects_unsupported_sort()
    {
        var validator = new ListAuthorsQueryValidator();

        var result = validator.Validate(new ListAuthorsQuery { Sort = "created" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Sort");
    }

    [Theory]
    [InlineData("title")]
    [InlineData("publishedYear")]
    public void ListBooksQueryValidator_accepts_supported_sort(string sort)
    {
        var validator = new ListBooksQueryValidator();

        var result = validator.Validate(new ListBooksQuery { Sort = sort, Direction = "desc" });

        result.IsValid.Should().BeTrue();
    }

    [Fact]
    public void ListBooksQueryValidator_rejects_unsupported_sort()
    {
        var validator = new ListBooksQueryValidator();

        var result = validator.Validate(new ListBooksQuery { Sort = "author" });

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(failure => failure.PropertyName == "Sort");
    }

    [Fact]
    public void PagedQueryValidator_accepts_query_without_sort_or_direction()
    {
        var validator = new ListBooksQueryValidator();

        var result = validator.Validate(new ListBooksQuery());

        result.IsValid.Should().BeTrue();
    }
}
