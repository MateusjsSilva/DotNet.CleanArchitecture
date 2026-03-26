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

        // All fields are optional — only validate those that are provided.
        // Name validation: only apply when name is not null, and if provided must not be empty
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name cannot be empty or whitespace.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.")
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
