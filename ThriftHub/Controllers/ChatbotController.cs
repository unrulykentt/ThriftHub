using Microsoft.AspNetCore.Mvc;
using ThriftHub.Services;

namespace ThriftHub.Controllers;

public class ChatbotController : Controller
{
    private readonly SiteHelpChatService _chatService;

    public ChatbotController(
        SiteHelpChatService chatService)
    {
        _chatService = chatService;
    }

    public sealed class ChatbotRequest
    {
        public string? Question { get; set; }
    }

    [HttpPost]
    public async Task<IActionResult> Ask(
        [FromBody] ChatbotRequest request,
        CancellationToken cancellationToken)
    {
        var reply =
            await _chatService.GetReplyAsync(
                request?.Question,
                cancellationToken);

        return Json(new
        {
            answer = reply.Answer,
            suggestions = reply.Suggestions
        });
    }
}
