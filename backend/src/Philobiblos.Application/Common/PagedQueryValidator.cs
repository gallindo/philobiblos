using FluentValidation;

namespace Philobiblos.Application.Common;

public abstract class PagedQueryValidator<T> : AbstractValidator<T> where T : PagedQuery
{
    protected PagedQueryValidator()
    {
        RuleFor(query => query.Page)
            .GreaterThanOrEqualTo(1)
            .WithMessage("Page must be at least 1.");

        RuleFor(query => query.PageSize)
            .InclusiveBetween(1, 100)
            .WithMessage("PageSize must be between 1 and 100.");

        RuleFor(query => query.Direction)
            .Must(direction => string.Equals(direction, "asc", StringComparison.OrdinalIgnoreCase)
                || string.Equals(direction, "desc", StringComparison.OrdinalIgnoreCase))
            .WithMessage("Direction must be 'asc' or 'desc'.")
            .When(query => query.Direction is not null);

        RuleFor(query => query.Direction)
            .Null()
            .WithMessage("Direction requires a sort field.")
            .When(query => query.Sort is null);
    }
}
