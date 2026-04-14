using CleanArchitecture.Application.Common.Mediator;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductsSummary;

internal sealed class GetProductsSummaryQueryHandler(IProductQueries productQueries)
    : IQueryHandler<GetProductsSummaryQuery, ProductsSummaryDto>
{
    public Task<ProductsSummaryDto> Handle(
        GetProductsSummaryQuery request,
        CancellationToken cancellationToken) =>
        productQueries.GetSummaryAsync(cancellationToken);
}
