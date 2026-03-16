# ADR-004: Manual Object Mapping (No AutoMapper / Mapster)

## Status
Accepted

## Context
Mapping domain entities to DTOs is required in almost every use case. Auto-mapping libraries (AutoMapper, Mapster) reduce boilerplate but introduce hidden conventions, runtime reflection, and refactoring friction.

## Decision
Use **explicit static extension methods** per feature for entity-to-DTO mapping. No mapping library dependency.

```csharp
// src/Application/UseCases/Products/ProductMappings.cs
internal static class ProductMappings
{
    internal static ProductDto ToDto(this Product product) => new(
        product.Id, product.Name, product.Description,
        product.Price, product.IsActive, product.CreatedAt);

    internal static IReadOnlyList<ProductDto> ToDtoList(this IEnumerable<Product> products) =>
        products.Select(p => p.ToDto()).ToList();
}
```

Handlers call `product.ToDto()` directly.

## Consequences
- **Positive**: Mappings are explicit and visible — no "magic" conventions to learn.
- **Positive**: Refactoring a property name is a compile-time error, not a silent runtime failure.
- **Positive**: Zero runtime overhead (no reflection).
- **Positive**: No NuGet dependency in the Application layer.
- **Negative**: More code to write per feature. Mitigated by the `dotnet new` template and the small size of typical DTO mappings.
