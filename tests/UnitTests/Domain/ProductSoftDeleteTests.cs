using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.UnitTests.Domain;

public sealed class ProductSoftDeleteTests
{
    [Fact]
    public void SoftDelete_ShouldSetIsDeletedToTrue()
    {
        var product = Product.Create("Test", null, 10m);

        product.SoftDelete();

        product.IsDeleted.Should().BeTrue();
    }

    [Fact]
    public void SoftDelete_ShouldSetDeletedAtTimestamp()
    {
        var before = DateTime.UtcNow;
        var product = Product.Create("Test", null, 10m);

        product.SoftDelete();

        product.DeletedAt.Should().NotBeNull();
        product.DeletedAt.Should().BeOnOrAfter(before);
    }

    [Fact]
    public void SoftDelete_WhenCalledTwice_ShouldNotOverrideDeletedAt()
    {
        var product = Product.Create("Test", null, 10m);

        product.SoftDelete();
        var firstDeletedAt = product.DeletedAt;

        product.SoftDelete();

        product.DeletedAt.Should().Be(firstDeletedAt);
    }
}
