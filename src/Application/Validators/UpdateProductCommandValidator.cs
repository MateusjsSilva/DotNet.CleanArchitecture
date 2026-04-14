using CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;
using CleanArchitecture.Application.Validators.Common;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation;

namespace CleanArchitecture.Application.Validators;

public sealed class UpdateProductCommandValidator : AbstractValidator<UpdateProductCommand>
{
    public UpdateProductCommandValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Id)
            .NotEmpty().WithMessage("Product id is required.");

        RuleFor(x => x.Name)
            .ValidateProductName();

        // Allow keeping the same name (excludeId skips the current product)
        RuleFor(x => x.Name)
            .MustAsync(async (cmd, name, ct) =>
                !await productRepository.ExistsByNameAsync(name, cmd.Id, ct))
            .WithMessage("A product with this name already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .ValidateProductDescription();

        RuleFor(x => x.Price)
            .ValidateProductPrice();

        RuleFor(x => x.RowVersion)
            .NotEmpty().WithMessage("Row version is required for concurrency control.");
    }
}
