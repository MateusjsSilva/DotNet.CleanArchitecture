using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Events;

namespace CleanArchitecture.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;

    private Product() { }

    public static Product Create(string name, string? description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        var product = new Product
        {
            Name = name,
            Description = description,
            Price = price
        };

        product.RaiseDomainEvent(new ProductCreatedEvent(product.Id, product.Name));

        return product;
    }

    public void Update(string name, string? description, decimal price)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price);

        Name = name;
        Description = description;
        Price = price;
    }

    /// <summary>
    /// Applies a partial update — only fields with a non-null value are changed.
    /// This supports HTTP PATCH semantics without requiring the caller to supply
    /// all fields.
    /// </summary>
    public void Patch(string? name, string? description, decimal? price)
    {
        if (name is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            Name = name;
        }

        if (description is not null)
            Description = description;

        if (price is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price.Value);
            Price = price.Value;
        }
    }

    public void Deactivate() => IsActive = false;
    public void Activate() => IsActive = true;
}
