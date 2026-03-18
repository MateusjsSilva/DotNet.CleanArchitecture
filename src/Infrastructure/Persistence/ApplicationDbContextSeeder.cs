using CleanArchitecture.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace CleanArchitecture.Infrastructure.Persistence;

/// <summary>
/// Seeds the database with sample data for development and demo purposes.
/// Safe to call multiple times — checks for existing data before inserting.
/// Usage in Program.cs (development only):
///   await ApplicationDbContextSeeder.SeedAsync(db);
/// </summary>
public static class ApplicationDbContextSeeder
{
    public static async Task SeedAsync(ApplicationDbContext context)
    {
        if (await context.Products.AnyAsync())
            return;

        var products = new[]
        {
            Product.Create("Laptop Pro 15", "High-performance laptop for professionals", 2_499.99m),
            Product.Create("Wireless Mouse", "Ergonomic wireless mouse with long battery life", 49.99m),
            Product.Create("Mechanical Keyboard", "Compact TKL mechanical keyboard with RGB lighting", 129.99m),
            Product.Create("USB-C Hub", "7-in-1 USB-C hub with 4K HDMI and 100W PD", 79.99m),
            Product.Create("Monitor 27\"", "4K IPS monitor with USB-C connectivity", 599.99m),
            Product.Create("Webcam 4K", "Professional webcam with auto-focus and noise cancellation", 199.99m),
            Product.Create("Laptop Stand", "Adjustable aluminum stand for better posture", 39.99m),
            Product.Create("USB-C Cable (3m)", "High-speed data and power delivery cable", 14.99m),
        };

        // Activate all products on seeding
        foreach (var product in products)
        {
            product.Activate();
            context.Products.Add(product);
        }

        await context.SaveChangesAsync();
    }
}
