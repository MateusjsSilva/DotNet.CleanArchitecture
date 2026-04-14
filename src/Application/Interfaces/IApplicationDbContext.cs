using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Application.Interfaces;

/// <summary>
/// Read-side abstraction over the database context.
/// Exposes only DbSet properties — never SaveChanges.
/// Commands must persist via <see cref="CleanArchitecture.Domain.Interfaces.IUnitOfWork"/>.
/// </summary>
public interface IApplicationDbContext
{
    DbSet<Product> Products { get; }
}
