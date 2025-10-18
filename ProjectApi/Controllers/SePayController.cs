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
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                Console.WriteLine("❌ Thiếu header Authorization");
                return Unauthorized("Missing Authorization header");
            }

            // ✅ Hỗ trợ cả 2 kiểu Authorization header
            var valid1 = authHeader == $"Key {SEPAY_API_KEY}";
            var valid2 = authHeader == $"Bearer {SEPAY_API_KEY}";

            if (!valid1 && !valid2)
            {
                Console.WriteLine($"❌ Sai API key. Nhận được: '{authHeader}'");
                return Unauthorized("Invalid API key");
            }


            Console.WriteLine("✅ Xác thực thành công webhook từ SePay.");
            Console.WriteLine("Dữ liệu nhận được:");

            foreach (var key in form.Keys)
                Console.WriteLine($" - {key}: {form[key]}");

            try
            {
                var amount = form["amount"].FirstOrDefault();
                var content = form["content"].FirstOrDefault();
                var reference = form["reference"].FirstOrDefault();

                Console.WriteLine($"💰 Giao dịch {reference} - {amount}đ - Nội dung: {content}");

                return Ok("OK");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Lỗi xử lý webhook: " + ex.Message);
                return BadRequest("Error");
            }
        }

    }
}
