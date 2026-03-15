using CleanArchitecture.Modules.AI.Interfaces;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.ChatCompletion;

namespace CleanArchitecture.Modules.AI.Services;

internal sealed class SemanticKernelService(Kernel kernel) : IAIService
{
    public async Task<string> CompleteAsync(string prompt, CancellationToken cancellationToken = default)
    {
        var result = await kernel.InvokePromptAsync(prompt, cancellationToken: cancellationToken);
        return result.ToString();
    }

    public async Task<string> ChatAsync(
        string systemMessage,
        string userMessage,
        CancellationToken cancellationToken = default)
    {
        var chatService = kernel.GetRequiredService<IChatCompletionService>();

        var history = new ChatHistory();
        history.AddSystemMessage(systemMessage);
        history.AddUserMessage(userMessage);

        var response = await chatService.GetChatMessageContentAsync(
            history,
            cancellationToken: cancellationToken);

        return response.Content ?? string.Empty;
    }
}
