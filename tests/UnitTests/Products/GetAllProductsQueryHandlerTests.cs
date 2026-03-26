using CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;
using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using NSubstitute;

namespace CleanArchitecture.UnitTests.Products;

public sealed class GetAllProductsQueryHandlerTests
{
    private readonly IProductRepository _productRepository;
    private readonly GetAllProductsQueryHandler _handler;

    public GetAllProductsQueryHandlerTests()
    {
        _productRepository = Substitute.For<IProductRepository>();
        _handler = new GetAllProductsQueryHandler(_productRepository);
    }

    // Helper: stub GetPagedAsync to return the given list as both items and totalCount
    private void SetupPaged(IReadOnlyList<Product> products)
    {
        _productRepository
            .GetPagedAsync(
                Arg.Any<bool>(), Arg.Any<string?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
                Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(),
                Arg.Any<CancellationToken>())
            .Returns((products, products.Count));
    }

    [Fact]
    public async Task Handle_WhenOnlyActiveIsTrue_ShouldReturnOnlyActiveProducts()
    {
        // Arrange
        var products = new List<Product> { Product.Create("Active Product", "Description", 10.00m) };
        SetupPaged(products);

        var query = new GetAllProductsQuery(OnlyActive: true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(1);
        result.Items[0].Name.Should().Be("Active Product");

        await _productRepository.Received(1).GetPagedAsync(
            Arg.Is<bool>(v => v == true),  // onlyActive
            Arg.Any<string?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenOnlyActiveIsFalse_ShouldReturnAllProducts()
    {
        // Arrange
        var products = new List<Product>
        {
            Product.Create("Product 1", null, 10.00m),
            Product.Create("Product 2", null, 20.00m)
        };
        SetupPaged(products);

        var query = new GetAllProductsQuery(OnlyActive: false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.TotalCount.Should().Be(2);

        await _productRepository.Received(1).GetPagedAsync(
            Arg.Is<bool>(v => v == false),  // onlyActive
            Arg.Any<string?>(), Arg.Any<decimal?>(), Arg.Any<decimal?>(),
            Arg.Any<string>(), Arg.Any<bool>(), Arg.Any<int>(), Arg.Any<int>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenNoProductsExist_ShouldReturnEmptyPagedResult()
    {
        // Arrange
        SetupPaged([]);

        var query = new GetAllProductsQuery();

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        result.Items.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }
}
