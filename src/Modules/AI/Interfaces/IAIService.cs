namespace CleanArchitecture.Modules.AI.Interfaces;

public interface IAIService
{
    /// <summary>Generates a text completion for the given prompt.</summary>
    Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default);

    /// <summary>Generates a structured chat response using a system message and user message.</summary>
    Task<string> ChatAsync(string systemMessage, string userMessage, CancellationToken cancellationToken = default);
}
