using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ProjectApi.Data;
using ProjectApi.Hubs;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SePayController : ControllerBase
    {
        private readonly IHubContext<PaymentsHub> _hub;
        private readonly FurnitureDbContext _context;
        private readonly string _webhookKey;

        public SePayController(IHubContext<PaymentsHub> hub, FurnitureDbContext context, IConfiguration cfg)
        {
            _hub = hub;
            _context = context;
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

            if (!body.TryGetProperty("transferAmount", out var amtProp) ||
                !body.TryGetProperty("content", out var contentProp))
            {
                return BadRequest("Thiếu trường cần thiết (transferAmount, content)");
            }

            var amount = amtProp.GetDecimal();
            var content = contentProp.GetString() ?? "";

            // ✅ Lấy orderId từ nội dung chuyển khoản: "DH_123"
            // ✅ Cho phép cả DH_11 và DH11
            var match = Regex.Match(content, @"DH[_\-]?(\d+)", RegexOptions.IgnoreCase);

            if (!match.Success)
            {
                Console.WriteLine("❌ Không tìm thấy mã đơn hàng trong nội dung: " + content);
                return Ok(new { success = false, message = "No order ID found" });
            }

            var orderId = int.Parse(match.Groups[1].Value);
            var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId);
            if (order == null)
            {
                Console.WriteLine($"❌ Không tìm thấy đơn hàng {orderId}");
                return NotFound(new { success = false, message = "Order not found" });
            }

            // ✅ Cập nhật trạng thái đơn hàng
            order.Status = "Paid";
            await _context.SaveChangesAsync();

            Console.WriteLine($"✅ Đơn hàng {orderId} đã thanh toán thành công ({amount}đ)");

            // ✅ Gửi tín hiệu realtime về frontend
            await _hub.Clients.Group($"DH_{orderId}").SendAsync("PaymentStatus", new
            {
                orderId,
                amount,
                content,
                status = "success"
            });


            return Ok(new { success = true, orderId });
        }
    }
}
