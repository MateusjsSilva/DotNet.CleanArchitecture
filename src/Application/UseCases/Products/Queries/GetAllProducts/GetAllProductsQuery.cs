using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery(
    bool OnlyActive = true,
    int Page = 1,
    int PageSize = 20) : IRequest<PagedResult<ProductDto>>;
