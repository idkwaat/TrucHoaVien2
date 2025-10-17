using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using ProjectApi.Data; // namespace DbContext của bạn
using ProjectApi.Models;
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

        public CassoController(FurnitureDbContext context, ILogger<CassoController> logger, IConfiguration config)
        {
            _context = context;
            _logger = logger;
            _config = config;
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> Webhook([FromBody] JsonElement data)
        {
            try
            {
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

                // ✅ Casso gửi trong field "data" (array of transactions)
                if (data.TryGetProperty("data", out JsonElement dataArray) && dataArray.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in dataArray.EnumerateArray())
                    {
                        decimal amount = item.GetProperty("amount").GetDecimal();
                        string description = item.GetProperty("description").GetString() ?? "";
                        string transactionId = item.GetProperty("id").GetString() ?? "";

                        int? orderId = TryParseOrderId(description);
                        if (orderId == null)
                        {
                            _logger.LogWarning($"⚠️ Không tìm được mã đơn hàng từ description: {description}");
                            continue;
                        }

                        var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
                        if (order == null)
                        {
                            _logger.LogWarning($"⚠️ Không tìm thấy đơn hàng ID={orderId}");
                            continue;
                        }

                        order.Status = "Paid";
                        order.PaymentTransactionId = transactionId;
                        order.PaymentAmount = amount;
                        order.PaidAt = DateTime.Now;

                        await _context.SaveChangesAsync();

                        _logger.LogInformation($"✅ Đơn hàng {order.Id} đã thanh toán thành công ({amount}đ)");
                    }
                }
                else
                {
                    _logger.LogWarning("⚠️ Payload không có trường 'data' hoặc không phải array");
                }

                return Ok(new { success = true });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "🔥 Lỗi xử lý webhook Casso");
                return BadRequest(new { success = false, message = ex.Message });
            }
        }


        // 🧩 Hàm phụ: trích ID đơn hàng từ description (VD: “Thanh toan DH_123”)
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
