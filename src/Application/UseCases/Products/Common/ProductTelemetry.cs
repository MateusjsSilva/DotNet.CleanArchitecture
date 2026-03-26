using System.Diagnostics.Metrics;

namespace CleanArchitecture.Application.UseCases.Products.Common;

/// <summary>
/// Centralized telemetry infrastructure for product-related operations.
/// Provides consistent metrics collection across all product event handlers.
/// </summary>
public static class ProductTelemetry
{
    private const string MeterName = "CleanArchitecture.Products";
    private const string Version = "1.0.0";

    /// <summary>
    /// Shared meter instance for all product-related metrics.
    /// </summary>
    public static readonly Meter Meter = new(MeterName, Version);

    /// <summary>
    /// Counter for product creation events.
    /// </summary>
    public static readonly Counter<int> CreatedCounter = Meter.CreateCounter<int>("products.created.count",
        description: "Total number of products created");

    /// <summary>
    /// Counter for product update events.
    /// </summary>
    public static readonly Counter<int> UpdatedCounter = Meter.CreateCounter<int>("products.updated.count",
        description: "Total number of products updated");

    /// <summary>
    /// Counter for product deletion events.
    /// </summary>
    public static readonly Counter<int> DeletedCounter = Meter.CreateCounter<int>("products.deleted.count",
        description: "Total number of products deleted");

    /// <summary>
    /// Counter for product patch events.
    /// </summary>
    public static readonly Counter<int> PatchedCounter = Meter.CreateCounter<int>("products.patched.count",
        description: "Total number of products patched");
}