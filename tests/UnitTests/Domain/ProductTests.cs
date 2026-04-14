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

    [Fact]
    public void Patch_WithAllNullFields_ShouldNotRaiseDomainEvent()
    {
        var product = Product.Create("Test", "Desc", 10m);
        product.ClearDomainEvents();

        product.Patch(null, null, null);

        product.DomainEvents.Should().BeEmpty();
    }

    [Fact]
    public void Patch_WithAllNullFields_ShouldNotChangeAnyField()
    {
        var product = Product.Create("Original", "Desc", 10m);
        product.ClearDomainEvents();

        product.Patch(null, null, null);

        product.Name.Should().Be("Original");
        product.Description.Should().Be("Desc");
        product.Price.Should().Be(10m);
    }

    [Fact]
    public void Patch_WithNameOnly_ShouldRaisePatchedEvent()
    {
        var product = Product.Create("Original", null, 10m);
        product.ClearDomainEvents();

        product.Patch("New Name", null, null);

        product.Name.Should().Be("New Name");
        product.Price.Should().Be(10m);
        product.DomainEvents.Should().ContainSingle();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Patch_WithEmptyName_ShouldThrowArgumentException(string emptyName)
    {
        var product = Product.Create("Valid", null, 10m);
        var act = () => product.Patch(emptyName, null, null);
        act.Should().Throw<ArgumentException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Patch_WithInvalidPrice_ShouldThrowArgumentOutOfRangeException(decimal invalidPrice)
    {
        var product = Product.Create("Valid", null, 10m);
        var act = () => product.Patch(null, null, invalidPrice);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }
}
