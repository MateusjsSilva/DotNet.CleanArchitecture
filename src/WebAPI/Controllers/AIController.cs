using CleanArchitecture.Modules.AI.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CleanArchitecture.WebAPI.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public sealed class AIController(IAIService aiService) : ControllerBase
{
    [HttpPost("complete")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Complete(
        [FromBody] PromptRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await aiService.CompleteAsync(request.Prompt, cancellationToken);
        return Ok(new { result });
    }

    [HttpPost("chat")]
    [ProducesResponseType<string>(StatusCodes.Status200OK)]
    public async Task<IActionResult> Chat(
        [FromBody] ChatRequest request,
        CancellationToken cancellationToken = default)
    {
        var result = await aiService.ChatAsync(
            request.SystemMessage, request.UserMessage, cancellationToken);

        return Ok(new { result });
    }

    public sealed record PromptRequest(string Prompt);
    public sealed record ChatRequest(string SystemMessage, string UserMessage);
}
