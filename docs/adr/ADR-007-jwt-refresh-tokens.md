# ADR-007: JWT Bearer + Refresh Token Rotation

## Status
Accepted

## Context
Stateless JWT tokens expire quickly for security. We need a mechanism to let users stay logged in without requiring frequent re-authentication, while still being able to revoke sessions.

## Decision
Use **short-lived JWT access tokens** (15 minutes) paired with **long-lived refresh tokens** (7 days) stored in the database with **rotation on every refresh**.

### Token flow

1. **Login / Register** — `AuthService` generates:
   - A signed JWT (HS256, 15 min expiry) with claims: `sub`, `email`, `jti`, roles.
   - A 64-byte secure random refresh token (Base64-encoded, 7-day expiry).
2. **Refresh** — `/auth/refresh` validates the refresh token (not expired, not revoked), then:
   - Issues a **new JWT** and a **new refresh token**.
   - Marks the old refresh token as revoked and stores `ReplacedByToken` for chain auditing.
3. **Revoke** — `/auth/revoke` explicitly marks a refresh token as revoked (logout).

### Refresh token entity

```csharp
public sealed class RefreshToken
{
    public string Token { get; init; }       // 64-byte Base64
    public DateTime ExpiresAt { get; init; }
    public DateTime? RevokedAt { get; set; }
    public string? ReplacedByToken { get; set; }

    public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
    public bool IsRevoked => RevokedAt is not null;
    public bool IsActive => !IsExpired && !IsRevoked;
}
```

### Security considerations
- `ClockSkew = TimeSpan.Zero` — no grace period on JWT expiry.
- Reuse of a revoked token is detectable via the `ReplacedByToken` chain (token family invalidation can be added on top).
- Refresh tokens are stored hashed in production-grade implementations; the template stores them as plain Base64 for simplicity — hash before storing in production.

## Consequences
- **Positive**: Short-lived access tokens limit the blast radius of token theft.
- **Positive**: Refresh token rotation ensures a stolen refresh token is detected on the next legitimate refresh.
- **Positive**: Full audit trail via `CreatedAt`, `RevokedAt`, `ReplacedByToken`.
- **Negative**: Requires database storage for refresh tokens (unlike fully stateless JWT).
