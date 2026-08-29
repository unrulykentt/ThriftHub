using Microsoft.AspNetCore.Mvc;

namespace ThriftHub.Controllers
{
    [ApiController]
    [Route("api/whatsapp/webhook")]
    public class WhatsAppWebhookController : ControllerBase
    {
        private const string VerifyToken = "thrifthub_webhook_2026";

        [HttpGet]
        public IActionResult Verify(
            [FromQuery(Name = "hub.mode")] string mode,
            [FromQuery(Name = "hub.verify_token")] string token,
            [FromQuery(Name = "hub.challenge")] string challenge)
        {
            if (mode == "subscribe" && token == VerifyToken)
            {
                return Content(challenge);
            }

            return Unauthorized();
        }

        [HttpPost]
        public IActionResult Receive([FromBody] object data)
        {
            return Ok();
        }
    }
}