using Azure.Core;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using System.Text.Json;


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
    public async Task<IActionResult> ReceiveWebhook()
    {
        // Log header
        Console.WriteLine("=== 📩 Headers từ SePay ===");
        foreach (var header in Request.Headers)
            Console.WriteLine($"{header.Key}: {header.Value}");
        Console.WriteLine("============================");

        // Xác thực API Key header
        var authHeader = Request.Headers["Authorization"].FirstOrDefault()?.Trim();
        if (string.IsNullOrEmpty(authHeader))
        {
            Console.WriteLine("❌ Thiếu header Authorization");
            return Unauthorized(new { success = false, message = "Missing Authorization header" });
        }
        // So sánh chính xác “Apikey ”
        if (!authHeader.Equals($"Apikey {SEPAY_API_KEY}", StringComparison.OrdinalIgnoreCase))
        {
            Console.WriteLine($"❌ Sai API key. Nhận được: '{authHeader}'");
            return Unauthorized(new { success = false, message = "Invalid API key" });
        }
        Console.WriteLine("✅ Xác thực API Key thành công!");

        // Đọc body JSON
        using var reader = new StreamReader(Request.Body);
        var body = await reader.ReadToEndAsync();
        Console.WriteLine("Body JSON từ SePay: " + body);

        JsonDocument doc;
        try
        {
            doc = JsonDocument.Parse(body);
        }
        catch (Exception ex)
        {
            Console.WriteLine("❌ Lỗi parse JSON: " + ex.Message);
            return BadRequest(new { success = false, message = "Invalid JSON" });
        }

        var root = doc.RootElement;
        // Lấy các trường cần thiết
        var id = root.GetProperty("id").GetInt32();
        var amount = root.GetProperty("transferAmount").GetDecimal();
        var referenceCode = root.GetProperty("referenceCode").GetString();
        var content = root.GetProperty("content").GetString();
        var transferType = root.GetProperty("transferType").GetString();
        // etc.

        Console.WriteLine($"Giao dịch id={id}, ref={referenceCode}, amount={amount}, type={transferType}");

        // Trả về JSON hợp lệ để SePay coi là thành công
        return StatusCode(200, new { success = true });
    }

}
}
