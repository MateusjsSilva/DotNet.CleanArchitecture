using CleanArchitecture.Domain.Common;
using CleanArchitecture.Domain.Events;

namespace CleanArchitecture.Domain.Entities;

public sealed class Product : AuditableEntity
{
    public string Name { get; private set; } = string.Empty;
    public string? Description { get; private set; }
    public decimal Price { get; private set; }
    public bool IsActive { get; private set; } = true;
    // Initialized to a non-null sentinel so EF Core InMemory (used in integration tests)
    // does not throw a nullability violation. SQL Server replaces this with a generated
    // rowversion on INSERT; InMemory keeps whatever value the entity carries.
    public byte[] RowVersion { get; private set; } = [0, 0, 0, 0, 0, 0, 0, 0];

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

        RaiseDomainEvent(new ProductUpdatedEvent(Id, Name, Price, Description));
    }

    /// <summary>
    /// Applies a partial update — only fields with a non-null value are changed.
    /// This supports HTTP PATCH semantics without requiring the caller to supply
    /// all fields.
    /// </summary>
    public void Patch(string? name, string? description, decimal? price)
    {
        var changedFields = new Dictionary<string, object?>();

        if (name is not null)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            changedFields["Name"] = Name; // Old value
            Name = name;
            changedFields["NewName"] = name;
        }

        if (description is not null)
        {
            changedFields["Description"] = Description; // Old value
            Description = description;
            changedFields["NewDescription"] = description;
        }

        if (price is not null)
        {
            ArgumentOutOfRangeException.ThrowIfNegativeOrZero(price.Value);
            changedFields["Price"] = Price; // Old value
            Price = price.Value;
            changedFields["NewPrice"] = price.Value;
        }

        if (changedFields.Count > 0)
        {
            RaiseDomainEvent(new ProductPatchedEvent(Id, Name, changedFields));
        }
    }

    public void Deactivate()
    {
        IsActive = false;
        RaiseDomainEvent(new ProductDeletedEvent(Id, Name));
    }

    public void Activate() => IsActive = true;
}
