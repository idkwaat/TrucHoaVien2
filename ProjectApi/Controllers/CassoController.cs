using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using ProjectApi.Data; // namespace DbContext của bạn
using ProjectApi.Models;
using Microsoft.EntityFrameworkCore;

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
        public async Task<IActionResult> Webhook([FromBody] dynamic data)
        {
            try
            {
                // ✅ Xác thực token
                string token = Request.Headers["Authorization"];
                string expected = "Bearer " + _config["Casso:Token"];

                if (token != expected)
                {
                    _logger.LogWarning("❌ Webhook token không hợp lệ!");
                    return Unauthorized();
                }

                // 🧾 Log dữ liệu
                string json = JsonConvert.SerializeObject(data);
                _logger.LogInformation($"📩 Nhận từ Casso: {json}");

                // 📦 Lấy thông tin chính
                decimal amount = data.amount;
                string description = data.description;
                string transactionId = data.transaction_id;

                // 💡 Giả sử bạn ghi "Thanh toan DH_123" trong mô tả khi tạo QR
                // -> ta tìm theo mã đơn hàng đó
                int? orderId = TryParseOrderId(description);
                if (orderId == null)
                {
                    _logger.LogWarning($"⚠️ Không tìm được mã đơn hàng từ description: {description}");
                    return Ok(new { success = false });
                }

                var order = await _context.Orders.FirstOrDefaultAsync(o => o.Id == orderId.Value);
                if (order == null)
                {
                    _logger.LogWarning($"⚠️ Không tìm thấy đơn hàng ID={orderId}");
                    return Ok(new { success = false });
                }

                // ✅ Cập nhật trạng thái
                order.Status = "Paid";
                order.PaymentTransactionId = transactionId;
                order.PaymentAmount = amount;
                order.PaidAt = DateTime.Now;

                await _context.SaveChangesAsync();

                _logger.LogInformation($"✅ Đơn hàng {order.Id} đã thanh toán thành công ({amount}đ)");

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
