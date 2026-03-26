using CleanArchitecture.Application.UseCases.Products.Commands.PatchProduct;
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
        RuleFor(x => x.Name)
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.")
            .When(x => x.Name is not null);

        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Product name cannot be empty or whitespace.")
            .When(x => x.Name is not null);

        // Check uniqueness only if the caller is supplying a new name
        RuleFor(x => x.Name)
            .MustAsync(async (cmd, name, ct) =>
                !await productRepository.ExistsByNameAsync(name!, cmd.Id, ct))
            .WithMessage("A product with this name already exists.")
            .When(x => !string.IsNullOrWhiteSpace(x.Name));

        RuleFor(x => x.Description)
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x.Description is not null);

        RuleFor(x => x.Price)
            .GreaterThan(0).WithMessage("Price must be greater than zero.")
            .When(x => x.Price.HasValue);
    }
}
