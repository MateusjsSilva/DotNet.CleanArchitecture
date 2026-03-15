using CleanArchitecture.Application.Interfaces;
using MediatR;

namespace CleanArchitecture.Application.UseCases.Products.Queries.GetProductsSummary;

internal sealed class GetProductsSummaryQueryHandler(IProductQueries productQueries)
    : IRequestHandler<GetProductsSummaryQuery, ProductsSummaryDto>
{
    public Task<ProductsSummaryDto> Handle(
        GetProductsSummaryQuery request,
        CancellationToken cancellationToken) =>
        productQueries.GetSummaryAsync(cancellationToken);
}
