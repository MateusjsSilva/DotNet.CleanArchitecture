using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductsSummary;

/// <summary>
/// Example of a Dapper read-side query for complex reporting / projections.
/// </summary>
public sealed record GetProductsSummaryQuery : IRequest<ProductsSummaryDto>;

public sealed record ProductsSummaryDto(
    int TotalProducts,
    int ActiveProducts,
    decimal AveragePrice,
    decimal TotalValue
);
