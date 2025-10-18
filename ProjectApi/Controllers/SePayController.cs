using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SePayController : ControllerBase
    {
        // 🔑 API key bạn nhập trong SePay (phải khớp 100%)
        // 🔑 API key bạn cấu hình trên SePay Dashboard
        private const string SEPAY_API_KEY = "Truchoavien"; // bỏ chữ "Key-" ra, chỉ giữ giá trị thực tế

        [HttpPost("webhook")]
        public IActionResult ReceiveWebhook([FromForm] IFormCollection form)
        {
            // ✅ Ghi log tất cả dữ liệu để debug
            Console.WriteLine("=== 📩 FORM DATA từ SePay ===");
            foreach (var key in form.Keys)
                Console.WriteLine($"{key}: {form[key]}");
            Console.WriteLine("============================");

            // 🔑 Lấy API key từ form
            var apiKey = form["api_key"].FirstOrDefault()?.Trim();

            // Nếu SePay không dùng trường api_key mà gửi thẳng key dưới tên khác,
            // thử lấy thêm một vài field phổ biến
            if (string.IsNullOrEmpty(apiKey))
            {
                apiKey = form["key"].FirstOrDefault()?.Trim()
                      ?? form["token"].FirstOrDefault()?.Trim()
                      ?? form["signature"].FirstOrDefault()?.Trim();
            }

            // ✅ Kiểm tra khớp với key bạn cấu hình trong SePay dashboard
            if (apiKey != SEPAY_API_KEY && apiKey != $"Key-{SEPAY_API_KEY}")
            {
                Console.WriteLine($"❌ Sai API key. Nhận được: '{apiKey}'");
                return Unauthorized("Invalid API key");
            }

            Console.WriteLine("✅ Xác thực API Key thành công!");

            // 👉 Xử lý nội dung giao dịch
            var amount = form["amount"].FirstOrDefault();
            var content = form["content"].FirstOrDefault();
            var reference = form["reference"].FirstOrDefault();

            Console.WriteLine($"💰 Giao dịch {reference} - {amount}đ - Nội dung: {content}");

            return Ok("OK");
        }


    }
}
