using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text;
using System.Text.Json;

namespace ProjectApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class PaymentsController : ControllerBase
    {
        private readonly IHttpClientFactory _httpFactory;
        private readonly IConfiguration _config;

        public PaymentsController(IHttpClientFactory httpFactory, IConfiguration config)
        {
            _httpFactory = httpFactory;
            _config = config;
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreatePayment([FromBody] CreatePaymentRequest req)
        {
            var orderRef = $"ORD-{DateTimeOffset.UtcNow.ToUnixTimeSeconds()}";

            var client = _httpFactory.CreateClient();
            client.DefaultRequestHeaders.Add("Authorization", $"Bearer {_config["SePay:ApiToken"]}");

            var payload = new
            {
                amount = req.Amount,
                content = req.Description,
                referenceCode = orderRef,
                note = "Thanh toan don hang",
            };

            var response = await client.PostAsync(
                _config["SePay:CreatePaymentUrl"],
                new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            );

            var body = await response.Content.ReadAsStringAsync();
            Console.WriteLine("📩 Phản hồi từ SePay: " + body);

            if (!response.IsSuccessStatusCode)
                return StatusCode((int)response.StatusCode, body);

            var json = JsonDocument.Parse(body).RootElement;
            var qr = json.GetProperty("data").GetProperty("qr_code").GetString();

            return Ok(new
            {
                reference = orderRef,
                qrCode = qr
            });
        }
    }

    public class CreatePaymentRequest
    {
        public decimal Amount { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
