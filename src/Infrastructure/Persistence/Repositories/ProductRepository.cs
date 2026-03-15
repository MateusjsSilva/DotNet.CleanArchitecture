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

    public async Task<bool> ExistsByNameAsync(
        string name,
        CancellationToken cancellationToken = default) =>
        await DbSet.AnyAsync(p => p.Name == name, cancellationToken);
}
