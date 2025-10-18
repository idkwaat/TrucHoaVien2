using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using ProjectApi.Hubs;
using System.Text.Json;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SePayController : ControllerBase
    {
        private readonly IHubContext<PaymentsHub> _hub;
        private readonly string _webhookKey;

        public SePayController(IHubContext<PaymentsHub> hub, IConfiguration cfg)
        {
            _hub = hub;
            _webhookKey = cfg["SePay:WebhookKey"] ?? string.Empty;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] JsonElement body)
        {
            var authHeader = Request.Headers["Authorization"].FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(authHeader) ||
                !authHeader.Equals($"Apikey {_webhookKey}", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Sai API key. Nhận được: {authHeader}");
                return Unauthorized("Invalid API key");
            }

            Console.WriteLine("📨 Webhook JSON: " + body.ToString());

            if (!body.TryGetProperty("referenceCode", out var refProp) ||
                !body.TryGetProperty("transferAmount", out var amtProp))
            {
                return BadRequest("Thiếu trường cần thiết");
            }

            var reference = refProp.GetString();
            var amount = amtProp.GetDecimal();
            var content = body.TryGetProperty("content", out var c) ? c.GetString() : "";

            Console.WriteLine($"✅ Nhận giao dịch: {reference} - {amount} - {content}");

            await _hub.Clients.Group(reference!).SendAsync("PaymentStatus", new
            {
                reference,
                amount,
                content,
                status = "success"
            });

            return Ok(new { success = true });
        }

    }
}
