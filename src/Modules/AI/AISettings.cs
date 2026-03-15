namespace CleanArchitecture.Modules.AI;

public sealed class AISettings
{
    public const string SectionName = "AISettings";

    public string Provider { get; init; } = "OpenAI"; // OpenAI | AzureOpenAI
    public string ModelId { get; init; } = "gpt-4o-mini";
    public string ApiKey { get; init; } = string.Empty;

    // Azure OpenAI specific
    public string? Endpoint { get; init; }
    public string? DeploymentName { get; init; }
}
