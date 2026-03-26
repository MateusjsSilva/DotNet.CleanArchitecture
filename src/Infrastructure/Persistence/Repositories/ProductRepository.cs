using CleanArchitecture.Domain.Entities;
using CleanArchitecture.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence.Repositories;

internal sealed class ProductRepository(ApplicationDbContext context)
    : BaseRepository<Product>(context), IProductRepository
{
    public async Task<IReadOnlyList<Product>> GetActiveProductsAsync(
        CancellationToken cancellationToken = default) =>
        await DbSet.AsNoTracking()
            .Where(p => p.IsActive)
            .ToListAsync(cancellationToken);

    public async Task<(IReadOnlyList<Product> Items, int TotalCount)> GetPagedAsync(
        bool onlyActive,
        string? nameContains,
        decimal? minPrice,
        decimal? maxPrice,
        string orderBy,
        bool ascending,
        int page,
        int pageSize,
        CancellationToken cancellationToken = default)
    {
        IQueryable<Product> query = DbSet.AsNoTracking();

        if (onlyActive)
            query = query.Where(p => p.IsActive);

        if (!string.IsNullOrWhiteSpace(nameContains))
        {
            var lower = nameContains.ToLowerInvariant();
            query = query.Where(p => p.Name.ToLower().Contains(lower));
        }

        if (minPrice.HasValue)
            query = query.Where(p => p.Price >= minPrice.Value);

        if (maxPrice.HasValue)
            query = query.Where(p => p.Price <= maxPrice.Value);

        var totalCount = await query.CountAsync(cancellationToken);

        query = orderBy.ToLowerInvariant() switch
        {
            "name"  => ascending ? query.OrderBy(p => p.Name)      : query.OrderByDescending(p => p.Name),
            "price" => ascending ? query.OrderBy(p => p.Price)     : query.OrderByDescending(p => p.Price),
            _       => ascending ? query.OrderBy(p => p.CreatedAt) : query.OrderByDescending(p => p.CreatedAt)
        };

        var items = await query
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync(cancellationToken);

        return (items, totalCount);
    }

    public async Task<bool> ExistsByNameAsync(
        string name,
        Guid? excludeId = null,
        CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(
            p => p.Name == name && (excludeId == null || p.Id != excludeId),
            cancellationToken);
}
