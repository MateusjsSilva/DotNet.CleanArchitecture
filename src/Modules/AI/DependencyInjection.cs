using CleanArchitecture.Modules.AI.Interfaces;
using CleanArchitecture.Modules.AI.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.SemanticKernel;

namespace CleanArchitecture.Modules.AI;

public static class DependencyInjection
{
    public static IServiceCollection AddAIModule(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var settings = configuration
            .GetSection(AISettings.SectionName)
            .Get<AISettings>();

        // If no AI settings are configured, register a no-op service
        if (settings is null || string.IsNullOrWhiteSpace(settings.ApiKey))
        {
            services.AddSingleton<IAIService, NoOpAIService>();
            return services;
        }

        var kernelBuilder = Kernel.CreateBuilder();

        if (settings.Provider.Equals("AzureOpenAI", StringComparison.OrdinalIgnoreCase))
        {
            kernelBuilder.AddAzureOpenAIChatCompletion(
                deploymentName: settings.DeploymentName ?? settings.ModelId,
                endpoint: settings.Endpoint!,
                apiKey: settings.ApiKey);
        }
        else
        {
            kernelBuilder.AddOpenAIChatCompletion(
                modelId: settings.ModelId,
                apiKey: settings.ApiKey);
        }

        services.AddSingleton(kernelBuilder.Build());
        services.AddScoped<IAIService, SemanticKernelService>();

        return services;
    }

    private sealed class NoOpAIService : IAIService
    {
        public Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default) =>
            Task.FromResult("AI module not configured. Set AISettings:ApiKey in appsettings.json.");

        public Task<string> ChatAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default) =>
            Task.FromResult("AI module not configured. Set AISettings:ApiKey in appsettings.json.");
    }
}
