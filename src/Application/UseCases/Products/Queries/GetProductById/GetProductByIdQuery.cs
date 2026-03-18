using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id)
    : IRequest<ProductDto?>, ICacheableQuery
{
    public string CacheKey => $"product:{Id}";

    /// <summary>
    /// Uses absolute expiration (10 minutes fixed).
    /// Suitable for product details that don't change frequently.
    /// </summary>
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);

    public TimeSpan? SlidingExpiration => null;
}
