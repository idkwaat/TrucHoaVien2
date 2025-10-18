using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class SePayController : ControllerBase
    {
        // 🔑 API key bạn nhập trong SePay (phải khớp 100%)
        private const string SEPAY_API_KEY = "Key-Truchoavien";

        [HttpPost("webhook")]
        public IActionResult ReceiveWebhook([FromForm] IFormCollection form)
        {
            // ✅ Xác thực API Key
            var authHeader = Request.Headers["Authorization"].FirstOrDefault();

            if (string.IsNullOrEmpty(authHeader))
            {
                Console.WriteLine("❌ Thiếu header Authorization");
                return Unauthorized("Missing Authorization header");
            }

            if (authHeader != $"Apikey {SEPAY_API_KEY}")
            {
                Console.WriteLine($"❌ Sai API key. Nhận: {authHeader}");
                return Unauthorized("Invalid API key");
            }

            // ✅ In ra toàn bộ dữ liệu nhận được
            Console.WriteLine("✅ Nhận webhook từ SePay (multipart/form-data):");
            foreach (var key in form.Keys)
            {
                Console.WriteLine($" - {key}: {form[key]}");
            }

            try
            {
                // Một số trường phổ biến mà SePay gửi
                var amount = form["amount"].FirstOrDefault();
                var content = form["content"].FirstOrDefault();
                var reference = form["reference"].FirstOrDefault();
                var bankCode = form["bank_code"].FirstOrDefault();
                var accountNumber = form["account_number"].FirstOrDefault();
                var transactionDate = form["transaction_date"].FirstOrDefault();

                Console.WriteLine($"💰 Giao dịch {reference} - {amount}đ - Nội dung: {content}");
                Console.WriteLine($"🏦 Ngân hàng: {bankCode}, TK: {accountNumber}, Ngày: {transactionDate}");

                // 👉 TODO: xử lý cập nhật đơn hàng tại đây (ví dụ tìm đơn theo mã DHxxx trong content)

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
