namespace CleanArchitecture.Application.DTOs;

public sealed record ProductDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    bool IsActive,
    DateTime CreatedAt,
    byte[] RowVersion
);
