namespace CleanArchitecture.Application.DTOs;

public sealed record AuthTokensDto(
    string AccessToken,
    string RefreshToken,
    DateTime AccessTokenExpiresAt
);
