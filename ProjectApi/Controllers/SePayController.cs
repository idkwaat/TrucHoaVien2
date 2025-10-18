using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SePayController : ControllerBase
    {
        // 🔑 API Key bạn đã nhập trong phần Webhook trên SePay Dashboard
        // (ví dụ: "Truchoavien123")
        private const string SEPAY_API_KEY = "Truchoavien123";

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook()
        {
            // 🧾 Log toàn bộ headers
            Console.WriteLine("=== 📩 Headers từ SePay ===");
            foreach (var header in Request.Headers)
                Console.WriteLine($"{header.Key}: {header.Value}");
            Console.WriteLine("============================");

            // ✅ Kiểm tra Authorization header
            var authHeader = Request.Headers["Authorization"].FirstOrDefault()?.Trim();

            if (string.IsNullOrEmpty(authHeader))
            {
                Console.WriteLine("❌ Thiếu header Authorization");
                return Unauthorized(new { success = false, message = "Missing Authorization header" });
            }

            // ✅ So khớp chính xác định dạng mà SePay gửi: "Apikey <API_KEY>"
            if (!authHeader.Equals($"Apikey {SEPAY_API_KEY}", StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine($"❌ Sai API key. Nhận được: '{authHeader}'");
                return Unauthorized(new { success = false, message = "Invalid API key" });
            }

            Console.WriteLine("✅ Xác thực API Key thành công!");

            // 🧠 Đọc body JSON
            string body;
            using (var reader = new StreamReader(Request.Body))
            {
                body = await reader.ReadToEndAsync();
            }

            if (string.IsNullOrWhiteSpace(body))
            {
                Console.WriteLine("⚠️ Body rỗng (SePay chưa gửi dữ liệu?)");
                return BadRequest(new { success = false, message = "Empty body" });
            }

            Console.WriteLine("📦 Body JSON từ SePay:");
            Console.WriteLine(body);

            try
            {
                var doc = JsonDocument.Parse(body);
                var root = doc.RootElement;

                // 🧩 Lấy các trường quan trọng trong webhook của SePay
                var id = root.GetProperty("id").GetInt32();
                var amount = root.GetProperty("transferAmount").GetDecimal();
                var referenceCode = root.GetProperty("referenceCode").GetString();
                var content = root.GetProperty("content").GetString();
                var transferType = root.GetProperty("transferType").GetString();

                Console.WriteLine($"💰 Giao dịch ID={id}, ref={referenceCode}, amount={amount}, type={transferType}");
                Console.WriteLine($"📝 Nội dung: {content}");

                // 👉 TODO: xử lý đơn hàng tại đây, ví dụ:
                // - Tìm đơn hàng trong DB theo mã trong `content`
                // - Cập nhật trạng thái thanh toán
                // - Gửi thông báo SignalR / Email nếu cần
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi parse JSON hoặc đọc dữ liệu: " + ex.Message);
                return BadRequest(new { success = false, message = "Invalid JSON format" });
            }

            // ✅ Trả về 200 để SePay biết webhook thành công
            return Ok(new { success = true });
        }
    }
}
