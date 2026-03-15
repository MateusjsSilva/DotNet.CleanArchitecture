using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Domain.Entities;

namespace CleanArchitecture.Application.UseCases.Products;

internal static class ProductMappings
{
    internal static ProductDto ToDto(this Product product) => new(
        product.Id,
        product.Name,
        product.Description,
        product.Price,
        product.IsActive,
        product.CreatedAt);

    internal static IReadOnlyList<ProductDto> ToDtoList(this IEnumerable<Product> products) =>
        products.Select(p => p.ToDto()).ToList();
}
