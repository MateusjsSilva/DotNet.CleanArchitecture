using Asp.Versioning;
using CleanArchitecture.Application.Common;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.UseCases.Products.Commands.CreateProduct;
using CleanArchitecture.Application.UseCases.Products.Commands.DeleteProduct;
using CleanArchitecture.Application.UseCases.Products.Commands.UpdateProduct;
using CleanArchitecture.Application.UseCases.Products.Queries.GetAllProducts;
using CleanArchitecture.Application.UseCases.Products.Queries.GetProductById;
using CleanArchitecture.Application.UseCases.Products.Queries.GetProductsSummary;
using CleanArchitecture.WebAPI.Models;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class ProductsController(ISender sender) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType<ApiResponse<PagedResult<ProductDto>>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetAll(
        [FromQuery] bool onlyActive = true,
        [FromQuery] int page = 1,
        [FromQuery] int pageSize = 20,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetAllProductsQuery(onlyActive, page, pageSize), cancellationToken);
        return Ok(new ApiResponse<PagedResult<ProductDto>>(result));
    }

    [HttpGet("{id:guid}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductByIdQuery(id), cancellationToken);
        return Ok(new ApiResponse<ProductDto>(result!));
    }

    [HttpPost]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Create(
        [FromBody] CreateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = result.Id }, new ApiResponse<ProductDto>(result));
    }

    [HttpPut("{id:guid}")]
    [ProducesResponseType<ApiResponse<ProductDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status422UnprocessableEntity)]
    public async Task<IActionResult> Update(
        Guid id,
        [FromBody] UpdateProductCommand command,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(command with { Id = id }, cancellationToken);
        return Ok(new ApiResponse<ProductDto>(result));
    }

    [HttpDelete("{id:guid}")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Delete(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        await sender.Send(new DeleteProductCommand(id), cancellationToken);
        return NoContent();
    }

    [HttpGet("summary")]
    [ProducesResponseType<ApiResponse<ProductsSummaryDto>>(StatusCodes.Status200OK)]
    public async Task<IActionResult> GetSummary(CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(new GetProductsSummaryQuery(), cancellationToken);
        return Ok(new ApiResponse<ProductsSummaryDto>(result));
    }
}
