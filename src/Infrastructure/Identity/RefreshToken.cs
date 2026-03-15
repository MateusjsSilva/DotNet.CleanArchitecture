namespace CleanArchitecture.Infrastructure.Identity;

public sealed class RefreshToken
{
    public Guid Id { get; init; } = Guid.CreateVersion7();
    public string Token { get; init; } = string.Empty;
    public DateTime ExpiresAt { get; init; }
    public DateTime CreatedAt { get; init; } = DateTime.UtcNow;
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public Guid UserId { get; init; }
    public ApplicationUser User { get; init; } = null!;

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt.HasValue;
    public bool IsActive => !IsRevoked && !IsExpired;
}
