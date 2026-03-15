using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Events;
using FluentAssertions;

namespace CleanArchitecture.UnitTests.Domain;

public sealed class ProductTests
{
    [Fact]
    public void Create_WithValidArguments_ShouldCreateProduct()
    {
        // Act
        var product = Product.Create("Test Product", "Description", 29.99m);

        // Assert
        product.Name.Should().Be("Test Product");
        product.Description.Should().Be("Description");
        product.Price.Should().Be(29.99m);
        product.IsActive.Should().BeTrue();
        product.Id.Should().NotBeEmpty();
    }

    [Fact]
    public void Create_ShouldRaiseProductCreatedDomainEvent()
    {
        // Act
        var product = Product.Create("Test Product", null, 10m);

        // Assert
        product.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<ProductCreatedEvent>();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_WithEmptyName_ShouldThrowArgumentException(string name)
    {
        // Act & Assert
        var act = () => Product.Create(name, null, 10m);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Create_WithInvalidPrice_ShouldThrowArgumentOutOfRangeException(decimal price)
    {
        // Act & Assert
        var act = () => Product.Create("Valid Name", null, price);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void Deactivate_ShouldSetIsActiveToFalse()
    {
        var product = Product.Create("Test", null, 10m);
        product.Deactivate();
        product.IsActive.Should().BeFalse();
    }
}
