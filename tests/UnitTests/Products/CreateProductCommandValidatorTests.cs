using CleanArchitecture.Application.UseCases.Products.Commands.CreateProduct;
using CleanArchitecture.Application.Validators;
using FluentAssertions;
using FluentValidation.TestHelper;

namespace CleanArchitecture.UnitTests.Products;

public sealed class CreateProductCommandValidatorTests
{
    private readonly CreateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command = new CreateProductCommand("Valid Name", "Description", 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
    {
        var command = new CreateProductCommand(name, null, 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenPriceIsZero_ShouldHaveValidationError()
    {
        var command = new CreateProductCommand("Valid Name", null, 0m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WhenPriceIsNegative_ShouldHaveValidationError()
    {
        var command = new CreateProductCommand("Valid Name", null, -1m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var longName = new string('A', 201);
        var command = new CreateProductCommand(longName, null, 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
