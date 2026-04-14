namespace CleanArchitecture.Application.DTOs;

public sealed record ProductsSummaryDto(
    int TotalProducts,
    int ActiveProducts,
    decimal AveragePrice,
    decimal TotalValue
);
