using CleanArchitecture.Application.UseCases.Products.Commands.CreateProduct;
using CleanArchitecture.Application.Validators;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation.TestHelper;
using NSubstitute;

namespace CleanArchitecture.UnitTests.Products;

public sealed class CreateProductCommandValidatorTests
{
    // Default stub: no product with the given name exists yet
    private static CreateProductCommandValidator BuildValidator(bool nameExists = false)
    {
        var repo = Substitute.For<IProductRepository>();
        repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(nameExists);
        return new CreateProductCommandValidator(repo);
    }

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command = new CreateProductCommand("Valid Name", "Description", 9.99m);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
    {
        var command = new CreateProductCommand(name, null, 9.99m);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenPriceIsZero_ShouldHaveValidationError()
    {
        var command = new CreateProductCommand("Valid Name", null, 0m);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task Validate_WhenPriceIsNegative_ShouldHaveValidationError()
    {
        var command = new CreateProductCommand("Valid Name", null, -1m);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task Validate_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var longName = new string('A', 201);
        var command = new CreateProductCommand(longName, null, 9.99m);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameAlreadyExists_ShouldHaveValidationError()
    {
        var command = new CreateProductCommand("Duplicate Name", null, 9.99m);
        var result = await BuildValidator(nameExists: true).TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }
}
