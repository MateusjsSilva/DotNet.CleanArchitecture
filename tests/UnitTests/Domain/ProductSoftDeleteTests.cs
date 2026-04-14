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
    public void SoftDelete_ShouldLeaveDeletedAtNull_UntilPersisted()
    {
        // DeletedAt is stamped by ApplicationDbContext.SetAuditFields() on SaveChanges,
        // not by SoftDelete() itself — keeping audit logic in one place.
        var product = Product.Create("Test", null, 10m);

        product.SoftDelete();

        product.DeletedAt.Should().BeNull();
    }

    [Fact]
    public void SoftDelete_WhenCalledTwice_ShouldNotChangeIsDeleted()
    {
        var product = Product.Create("Test", null, 10m);

        product.SoftDelete();
        product.SoftDelete(); // idempotent — should not throw or change state

        product.IsDeleted.Should().BeTrue();
        product.DeletedAt.Should().BeNull(); // still null until SaveChanges
    }
}
