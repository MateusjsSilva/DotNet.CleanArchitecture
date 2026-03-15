using CleanArchitecture.Application.DTOs;

namespace CleanArchitecture.Application.Interfaces;

public interface IAuthService
{
    Task<AuthTokensDto> LoginAsync(string email, string password, CancellationToken cancellationToken = default);
    Task<AuthTokensDto> RefreshAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task RevokeAsync(string refreshToken, CancellationToken cancellationToken = default);
    Task<AuthTokensDto> RegisterAsync(string email, string password, string? fullName, CancellationToken cancellationToken = default);
}
