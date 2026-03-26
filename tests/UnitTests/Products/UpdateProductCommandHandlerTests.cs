using CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Exceptions;
using CleanArchitecture.Domain.Interfaces;
using NSubstitute;

namespace CleanArchitecture.UnitTests.Products;

public sealed class UpdateProductCommandHandlerTests
{
    private readonly IProductRepository _productRepository = Substitute.For<IProductRepository>();
    private readonly IUnitOfWork _unitOfWork = Substitute.For<IUnitOfWork>();
    private readonly UpdateProductCommandHandler _handler;
    private static readonly byte[] TestRowVersion = [1, 2, 3, 4, 5, 6, 7, 8];

    public UpdateProductCommandHandlerTests()
    {
        _handler = new UpdateProductCommandHandler(_productRepository, _unitOfWork);
    }

    [Fact]
    public async Task Handle_WhenProductExists_ShouldUpdateAndReturnDto()
    {
        // Arrange
        var product = Product.Create("Old Name", "Old Desc", 10m);
        // Simulate entity with RowVersion from database
        typeof(Product).GetProperty("RowVersion")!.SetValue(product, TestRowVersion);

        _productRepository
            .GetByIdAsync(product.Id, Arg.Any<CancellationToken>())
            .Returns(product);

        var command = new UpdateProductCommand(product.Id, "New Name", "New Desc", 99m, TestRowVersion);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        result.Name.Should().Be("New Name");
        result.Description.Should().Be("New Desc");
        result.Price.Should().Be(99m);

        _productRepository.Received(1).Update(product);
        await _unitOfWork.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenProductNotFound_ShouldThrowNotFoundException()
    {
        // Arrange
        _productRepository
            .GetByIdAsync(Arg.Any<Guid>(), Arg.Any<CancellationToken>())
            .Returns((Product?)null);

        var command = new UpdateProductCommand(Guid.NewGuid(), "Name", null, 10m, TestRowVersion);

        // Act & Assert
        var act = async () => await _handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotFoundException>();
    }
}
