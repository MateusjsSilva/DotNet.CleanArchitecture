using CleanArchitecture.Domain.Interfaces;

namespace CleanArchitecture.Infrastructure.Persistence;

internal sealed class UnitOfWork(ApplicationDbContext context) : IUnitOfWork
{
    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) =>
        context.SaveChangesAsync(cancellationToken);
}
