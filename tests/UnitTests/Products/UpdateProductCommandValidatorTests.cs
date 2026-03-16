using CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;
using CleanArchitecture.Application.Validators;
using FluentValidation.TestHelper;

namespace CleanArchitecture.UnitTests.Products;

public sealed class UpdateProductCommandValidatorTests
{
    private readonly UpdateProductCommandValidator _validator = new();

    [Fact]
    public void Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", "Description", 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public void Validate_WhenIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.Empty, "Valid Name", null, 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), name, null, 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), new string('A', 201), null, 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public void Validate_WhenDescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", new string('A', 1001), 9.99m);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Validate_WhenPriceIsInvalid_ShouldHaveValidationError(decimal price)
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", null, price);
        var result = _validator.TestValidate(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }
}
