using CleanArchitecture.Application.Interfaces;
using Microsoft.AspNetCore.Http;
using System.Security.Claims;

namespace CleanArchitecture.Infrastructure.Services;

internal sealed class CurrentUserService(IHttpContextAccessor httpContextAccessor)
    : ICurrentUserService
{
    private ClaimsPrincipal? User => httpContextAccessor.HttpContext?.User;

    public string? UserId =>
        User?.FindFirstValue(ClaimTypes.NameIdentifier);

    public string? UserName =>
        User?.FindFirstValue(ClaimTypes.Email)
        ?? User?.FindFirstValue(ClaimTypes.Name);

    public bool IsAuthenticated =>
        User?.Identity?.IsAuthenticated is true;
}
