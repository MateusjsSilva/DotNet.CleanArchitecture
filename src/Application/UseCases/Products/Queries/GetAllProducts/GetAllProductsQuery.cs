using CleanArchitecture.Application.DTOs;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;

public sealed record GetAllProductsQuery(bool OnlyActive = true) : IRequest<IReadOnlyList<ProductDto>>;
