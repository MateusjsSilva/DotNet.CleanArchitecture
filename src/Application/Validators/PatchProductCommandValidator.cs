using CleanArchitecture.Application.UseCases.Products.Commands.PatchProduct;
using CleanArchitecture.Application.Validators.Common;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation;

namespace CleanArchitecture.Application.Validators;

public sealed class PatchProductCommandValidator : AbstractValidator<PatchProductCommand>
{
    public PatchProductCommandValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for concurrency control.");

        // All fields are optional — only validate those that are provided.
        RuleFor(x => x.Name!)
            .ValidateProductName()
            .When(x => x.Name is not null);

        // Check uniqueness only if the caller is supplying a new name
        RuleFor(x => x.Name)
            .MustAsync(async (cmd, name, ct) =>
                string.IsNullOrWhiteSpace(name) ||
                !await productRepository.ExistsByNameAsync(name, cmd.Id, ct))
            .WithMessage("A product with this name already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .ValidateProductDescription();

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .When(x => x.Price.HasValue);
    }
}
