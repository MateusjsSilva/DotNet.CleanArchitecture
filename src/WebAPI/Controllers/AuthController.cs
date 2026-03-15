using Asp.Versioning;
using CleanArchitecture.Application.DTOs;
using CleanArchitecture.Application.Interfaces;
using CleanArchitecture.WebAPI.Models;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

[ApiController]
[ApiVersion(1)]
[Route("api/v{version:apiVersion}/[controller]")]
public sealed class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    [ProducesResponseType<ApiResponse<AuthTokensDto>>(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    public async Task<IActionResult> Register(
        [FromBody] RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokens = await authService.RegisterAsync(
            request.Email, request.Password, request.FullName, cancellationToken);

        return StatusCode(StatusCodes.Status201Created, new ApiResponse<AuthTokensDto>(tokens));
    }

    [HttpPost("login")]
    [ProducesResponseType<ApiResponse<AuthTokensDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Login(
        [FromBody] LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokens = await authService.LoginAsync(request.Email, request.Password, cancellationToken);
        return Ok(new ApiResponse<AuthTokensDto>(tokens));
    }

    [HttpPost("refresh")]
    [ProducesResponseType<ApiResponse<AuthTokensDto>>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Refresh(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        var tokens = await authService.RefreshAsync(request.RefreshToken, cancellationToken);
        return Ok(new ApiResponse<AuthTokensDto>(tokens));
    }

    [HttpPost("revoke")]
    [ProducesResponseType(StatusCodes.Status204NoContent)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    public async Task<IActionResult> Revoke(
        [FromBody] RefreshRequest request,
        CancellationToken cancellationToken = default)
    {
        await authService.RevokeAsync(request.RefreshToken, cancellationToken);
        return NoContent();
    }

    public sealed record RegisterRequest(string Email, string Password, string? FullName);
    public sealed record LoginRequest(string Email, string Password);
    public sealed record RefreshRequest(string RefreshToken);
}
