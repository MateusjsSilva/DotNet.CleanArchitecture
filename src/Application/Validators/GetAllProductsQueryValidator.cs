using CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;
using FluentValidation;

namespace CleanArchitecture.Application.Validators;

public sealed class GetAllProductsQueryValidator : AbstractValidator<GetAllProductsQuery>
{
    // Stored lower-cased so the Contains check is case-insensitive without extra allocation.
    private static readonly string[] AllowedOrderByFields = ["createdat", "name", "price"];

    public GetAllProductsQueryValidator()
    {
        RuleFor(x => x.Page)
            .GreaterThan(0).WithMessage("Page must be greater than 0.");

        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("PageSize must be greater than 0.")
            .LessThanOrEqualTo(100).WithMessage("PageSize cannot exceed 100.");

        RuleFor(x => x.OrderBy)
            .Must(o => AllowedOrderByFields.Contains(o.ToLowerInvariant()))
            .WithMessage("OrderBy must be one of: createdAt, name, price.");
    }
}
