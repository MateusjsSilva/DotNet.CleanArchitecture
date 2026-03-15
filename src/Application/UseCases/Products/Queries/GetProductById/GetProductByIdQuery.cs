using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductById;

public sealed record GetProductByIdQuery(Guid Id)
    : IRequest<ProductDto?>, ICacheableQuery
{
    public string CacheKey => $"product:{Id}";
    public TimeSpan? AbsoluteExpiration => TimeSpan.FromMinutes(10);
}
