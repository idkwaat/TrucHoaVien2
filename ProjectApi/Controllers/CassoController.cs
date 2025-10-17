using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.SignalR;
using ProjectApi.Data;
using ProjectApi.Models;
using ProjectApi.Hubs;
using System.Text.Json;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class CassoController : ControllerBase
    {
        private readonly FurnitureDbContext _context;
        private readonly ILogger<CassoController> _logger;
        private readonly IConfiguration _config;
        private readonly IHubContext<PaymentsHub> _hub;

        public CassoController(
            FurnitureDbContext context,
            ILogger<CassoController> logger,
            IConfiguration config,
            IHubContext<PaymentsHub> hub)
        {
            _context = context;
            _logger = logger;
            _config = config;
            _hub = hub;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement data)
        {
            try
            {
                // ✅ Kiểm tra token xác thực webhook
                string token = Request.Headers["X-Webhook-Token"];
                string expected = _config["Casso:Token"];

                if (string.IsNullOrEmpty(token))
                {
                    _logger.LogWarning("⚠️ Không có header X-Webhook-Token (có thể do gọi thử). Bỏ qua xác thực.");
                }
                else if (token != expected)
                {
                    _logger.LogWarning("❌ Webhook token không hợp lệ!");
                    return Unauthorized();
                }

                _logger.LogInformation($"📩 Nhận từ Casso: {data}");

                // ✅ Kiểm tra và đọc trường "data"
                if (data.TryGetProperty("data", out JsonElement dataElement))
                {
                    if (dataElement.ValueKind == JsonValueKind.Array)
                    {
                        // Casso gửi nhiều giao dịch cùng lúc
                        foreach (var item in dataElement.EnumerateArray())
                        {
                            await HandleTransaction(item);
                        }
                    }
                    else if (dataElement.ValueKind == JsonValueKind.Object)
                    {
                        // Casso gửi 1 giao dịch duy nhất
                        await HandleTransaction(dataElement);
                    }
                    else
                    {
                        _logger.LogWarning("⚠️ 'data' không phải object hoặc array hợp lệ");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Payload không có trường 'data'");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Lỗi xử lý webhook Casso");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }

        // 🧩 Hàm xử lý từng giao dịch Casso
        private async Task HandleTransaction(JsonElement item)
        {
            try
            {
                decimal amount = item.GetProperty("amount").GetDecimal();
                string description = item.GetProperty("description").GetString() ?? "";
                string transactionId = item.GetProperty("id").GetRawText();

                int? orderId = TryParseOrderId(description);
                if (orderId == null)
                {
                    _logger.LogWarning($"⚠️ Không tìm được mã đơn hàng từ description: {description}");
                    return;
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
                if (order == null)
                {
                    _logger.LogWarning($"⚠️ Không tìm thấy đơn hàng ID={orderId}");
                    return;
                }

                // ✅ Cập nhật trạng thái đơn hàng
                order.Status = "Paid";
                order.PaymentTransactionId = transactionId;
                order.PaymentAmount = amount;
                order.PaidAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Đơn hàng {order.Id} đã thanh toán thành công ({amount}đ)");

                // ✅ Gửi thông báo realtime qua SignalR
                await _hub.Clients.Group($"order-{order.Id}")
                    .SendAsync("PaymentSuccess", new
                    {
                        orderId = order.Id,
                        amount,
                        message = "Thanh toán thành công"
                    });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Lỗi xử lý transaction Casso");
            }
        }

        // 🧩 Trích ID đơn hàng từ mô tả (VD: "DH_123" hoặc "DH123")
        private int? TryParseOrderId(string desc)
        {
            try
            {
                var match = System.Text.RegularExpressions.Regex.Match(desc, @"DH[_\-]?(\d+)");
                if (match.Success && int.TryParse(match.Groups[1].Value, out int id))
                    return id;
            }
            catch { }
            return null;
        }
    }
}
