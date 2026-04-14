using CleanArchitecture.Application.UseCases.Products.Commands.CreateProduct;
using CleanArchitecture.Application.Validators.Common;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation;

namespace CleanArchitecture.Application.Validators;

public sealed class CreateProductCommandValidator : AbstractValidator<CreateProductCommand>
{
    public CreateProductCommandValidator(IProductRepository productRepository)
    {
        RuleFor(x => x.Name)
            .ValidateProductName();

        // Only hit the DB when the basic name rules pass
        RuleFor(x => x.Name)
            .MustAsync(async (name, ct) => !await productRepository.ExistsByNameAsync(name, cancellationToken: ct))
            .WithMessage("A product with this name already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .ValidateProductDescription();

        RuleFor(x => x.Price)
            .ValidateProductPrice();
    }
}
