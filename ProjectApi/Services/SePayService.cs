using Microsoft.Extensions.Options;
using ProjectApi.Models;
using System.Net.Http;
using System.Text.Json;

public class SePayService
{
    private readonly HttpClient _http;
    private readonly SePaySettings _settings;

    public SePayService(HttpClient http, IOptions<SePaySettings> settings)
    {
        _http = http;
        _settings = settings.Value;

        // SePay docs show Authorization: Apikey <API_TOKEN> for API calls
        if (!_http.DefaultRequestHeaders.Contains("Authorization"))
            _http.DefaultRequestHeaders.Add("Authorization", $"Apikey {_settings.ApiToken}");
    }

    // Ví dụ: Lấy giao dịch theo reference (theo docs)
    public async Task<JsonElement?> GetTransactionByReferenceAsync(string reference)
    {
        var url = $"https://my.sepay.vn/userapi/transactions/list?reference_number={Uri.EscapeDataString(reference)}";
        var res = await _http.GetAsync(url);
        if (!res.IsSuccessStatusCode) return null;

        var body = await res.Content.ReadAsStringAsync();
        var doc = JsonDocument.Parse(body);
        if (doc.RootElement.TryGetProperty("transactions", out var arr) && arr.GetArrayLength() > 0)
            return arr[0];
        return null;
    }
}
