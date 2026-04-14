using FluentValidation;
using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Application.Validators.Common;

/// <summary>
/// Shared validation rules for product-related commands.
/// Eliminates duplication across Create, Update, and Patch validators.
/// </summary>
public static class ProductValidationRules
{
    /// <summary>
    /// Validates product name: required, non-empty, max 200 characters.
    /// </summary>
    public static IRuleBuilderOptions<T, string> ValidateProductName<T>(
        this IRuleBuilder<T, string> ruleBuilder)
    {
        return ruleBuilder
            .NotEmpty().WithMessage("Product name is required.")
            .MaximumLength(200).WithMessage("Product name must not exceed 200 characters.");
    }

    /// <summary>
    /// Validates product price: must be greater than zero.
    /// </summary>
    public static IRuleBuilderOptions<T, decimal> ValidateProductPrice<T>(
        this IRuleBuilder<T, decimal> ruleBuilder)
    {
        return ruleBuilder
            .GreaterThan(0).WithMessage("Price must be greater than zero.");
    }

    /// <summary>
    /// Validates product description: optional, max 1000 characters when provided.
    /// </summary>
    public static IRuleBuilderOptions<T, string?> ValidateProductDescription<T>(
        this IRuleBuilder<T, string?> ruleBuilder)
    {
        return ruleBuilder
            .MaximumLength(1000).WithMessage("Description must not exceed 1000 characters.")
            .When(x => x != null);
    }
}

