using CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;
using CleanArchitecture.Application.Validators;
using CleanArchitecture.Domain.Interfaces;
using FluentValidation.TestHelper;
using NSubstitute;

namespace CleanArchitecture.UnitTests.Products;

public sealed class UpdateProductCommandValidatorTests
{
    private static readonly byte[] TestRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];
    private static UpdateProductCommandValidator BuildValidator(bool nameExists = false)
    {
        var repo = Substitute.For<IProductRepository>();
        repo.ExistsByNameAsync(Arg.Any<string>(), Arg.Any<Guid?>(), Arg.Any<CancellationToken>())
            .Returns(nameExists);
        return new UpdateProductCommandValidator(repo);
    }

    [Fact]
    public async Task Validate_WhenCommandIsValid_ShouldNotHaveErrors()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", "Description", 9.99m, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldNotHaveAnyValidationErrors();
    }

    [Fact]
    public async Task Validate_WhenIdIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.Empty, "Valid Name", null, 9.99m, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Id);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Validate_WhenNameIsEmpty_ShouldHaveValidationError(string name)
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), name, null, 9.99m, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenNameExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), new string('A', 201), null, 9.99m, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenDescriptionExceedsMaxLength_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", new string('A', 1001), 9.99m, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Description);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public async Task Validate_WhenPriceIsInvalid_ShouldHaveValidationError(decimal price)
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", null, price, TestRowVersion);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Price);
    }

    [Fact]
    public async Task Validate_WhenNameAlreadyExistsForDifferentProduct_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Taken Name", null, 9.99m, TestRowVersion);
        var result = await BuildValidator(nameExists: true).TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.Name);
    }

    [Fact]
    public async Task Validate_WhenRowVersionIsEmpty_ShouldHaveValidationError()
    {
        var command = new UpdateProductCommand(Guid.NewGuid(), "Valid Name", null, 9.99m, []);
        var result = await BuildValidator().TestValidateAsync(command);
        result.ShouldHaveValidationErrorFor(x => x.RowVersion);
    }
}
