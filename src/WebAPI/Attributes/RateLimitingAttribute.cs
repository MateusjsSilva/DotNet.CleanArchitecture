using CleanArchitecture.WebAPI.Extensions;
using Microsoft.AspNetCore.RateLimiting;

namespace CleanArchitecture.WebAPI.Attributes;

/// <summary>
/// Applies a specific granular rate limiting policy to a controller.
/// Usage: [GranularRateLimit(RateLimitPolicies.Auth)]
/// </summary>
[AttributeUsage(AttributeTargets.Class)]
public sealed class GranularRateLimitAttribute(string policyName) : Attribute
{
    /// <summary>The rate limiting policy name to apply (e.g., "auth", "products", "ai").</summary>
    public string PolicyName => policyName;
}

/// <summary>
/// Preset policies for common rate limiting scenarios.
/// </summary>
public static class RateLimitPolicies
{
    public const string Auth = RateLimitingExtensions.AuthPolicy;       // 5 req/min - brute force protection
    public const string Products = RateLimitingExtensions.ProductsPolicy; // 100 req/min - standard CRUD
    public const string AI = RateLimitingExtensions.AIPolicy;           // 30 req/min - resource-intensive
    public const string Default = RateLimitingExtensions.DefaultPolicy;  // 50 req/min - fallback
}
