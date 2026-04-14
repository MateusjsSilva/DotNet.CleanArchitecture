using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Domain.Interfaces;

public interface IProductRepository : IRepository<Product>
{
    Task<IReadOnlyList<Product>> GetActiveProductsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns a filtered, sorted, and paginated slice of products, with the total count
    /// computed at the database level — no in-memory post-processing.
    /// </summary>
    Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        bool onlyActive,
        string? nameContains,
        decimal? minPrice,
        decimal? maxPrice,
        string orderBy,
        bool ascending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Returns true if any product has the given name, optionally ignoring <paramref name="excludeId"/>.
    /// Pass the current product's id when updating to allow keeping the same name.
    /// </summary>
    Task<bool> ExistsByNameAsync(string name, Guid? excludeId = null, CancellationToken cancellationToken = default);
}
