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
        public async Task<IActionResult> ReceiveWebhook()
        {
            // 🧩 In tất cả headers để debug
            Console.WriteLine("=== 📩 Headers từ SePay ===");
            foreach (var h in Request.Headers)
                Console.WriteLine($"{h.Key}: {h.Value}");
            Console.WriteLine("============================");

            // ✅ Kiểm tra Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault()?.Trim();
            if (string.IsNullOrEmpty(authHeader))
            {
                Console.WriteLine("❌ Thiếu header Authorization");
                return Unauthorized("Missing Authorization header");
            }

            // ✅ So khớp chính xác với cấu hình trong appsettings.json
            if (!authHeader.Equals($"Apikey {_webhookKey}", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Sai API key. Nhận được: {authHeader}");
                return Unauthorized("Invalid API key");
            }

            Console.WriteLine("✅ Xác thực API Key thành công!");

            // ✅ Đọc body JSON
            using var reader = new StreamReader(Request.Body);
            var body = await reader.ReadToEndAsync();

            if (string.IsNullOrWhiteSpace(body))
                return BadRequest("Empty body");

            Console.WriteLine("📨 Body JSON từ SePay: " + body);

            JsonDocument doc;
            try
            {
                doc = JsonDocument.Parse(body);
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi parse JSON: " + ex.Message);
                return BadRequest("Invalid JSON");
            }

            var json = doc.RootElement;

            // ✅ Lấy dữ liệu từ SePay webhook
            string? reference = json.GetProperty("referenceCode").GetString();
            decimal amount = json.GetProperty("transferAmount").GetDecimal();
            string? content = json.GetProperty("content").GetString();

            Console.WriteLine($"💰 Nhận giao dịch: {reference} - {amount}đ - Nội dung: {content}");

            // ✅ Phát sự kiện realtime qua SignalR
            if (!string.IsNullOrEmpty(reference))
            {
                await _hub.Clients.Group(reference).SendAsync("PaymentStatus", new
                {
                    reference,
                    amount,
                    content,
                    status = "success"
                });
            }

            // ✅ Trả về OK để SePay ghi nhận webhook thành công
            return Ok(new { success = true });
        }
    }
}
