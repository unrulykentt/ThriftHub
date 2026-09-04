using Microsoft.AspNetCore.Mvc;
using ThriftHub.Services;

namespace ThriftHub.Controllers;

public class ChatbotController : Controller
{
    public sealed class ChatbotRequest
    {
        public string? Question { get; set; }
    }

    [HttpPost]
    public IActionResult Ask(
        [FromBody] ChatbotRequest request)
    {
        var reply =
            SiteHelpChatService.GetReply(
                request?.Question);

        return Json(new
        {
            answer = reply.Answer,
            suggestions = reply.Suggestions
        });
    }
}
