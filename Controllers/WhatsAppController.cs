using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

[ApiController]
[Route("api/whatsapp")]
public class WhatsAppController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly HttpClient _httpClient;

    public WhatsAppController(IConfiguration configuration, HttpClient httpClient)
    {
        _configuration = configuration;
        _httpClient = httpClient;
    }

    [HttpPost("send-message")]
    public async Task<IActionResult> SendMessage([FromForm] WhatsAppRequest request)
    {
        string metaToken = request.MetaToken ?? _configuration["META_TOKEN"];
        if (string.IsNullOrEmpty(metaToken))
        {
            return BadRequest(new { error = "Missing META token" });
        }

        var payload = new
        {
            messaging_product = "whatsapp",
            to = request.EndUserNumber,
            text = new { body = request.Message }
        };

        var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{request.BusinessPhoneNumberId}/messages")
        {
            Headers = { { "Authorization", $"Bearer {metaToken}" } },
            Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
        };

        var response = await _httpClient.SendAsync(httpRequest);
        var responseData = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
            return BadRequest(new { error = "Failed to send message: " + responseData });
        }

        return Ok(new { success = "Message sent successfully!" });
    }

    [HttpGet("webhook")]
    public IActionResult VerifyWebhook([FromQuery] string hub_mode, [FromQuery] string hub_verify_token, [FromQuery] string hub_challenge)
    {
        if (hub_mode == "subscribe" && hub_verify_token == "abc123")
        {
            return Content(hub_challenge, "text/plain");
        }
        return Forbid();
    }

    [HttpPost("webhook")]
    public async Task<IActionResult> ReceiveWebhook([FromBody] JsonDocument body)
    {
        if (string.IsNullOrEmpty(_configuration["META_TOKEN"]))
        {
            return BadRequest(new { error = "Missing META_TOKEN environment variable" });
        }

        var message = body.RootElement.GetProperty("entry")[0].GetProperty("changes")[0].GetProperty("value").GetProperty("messages")[0];
        var phoneNumberId = body.RootElement.GetProperty("entry")[0].GetProperty("changes")[0].GetProperty("value").GetProperty("metadata").GetProperty("phone_number_id").GetString();

        if (message.GetProperty("type").GetString() == "text")
        {
            var responsePayload = new
            {
                messaging_product = "whatsapp",
                to = message.GetProperty("from").GetString(),
                text = new { body = "Echo: " + message.GetProperty("text").GetProperty("body").GetString() },
                context = new { message_id = message.GetProperty("id").GetString() }
            };

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"https://graph.facebook.com/v22.0/{phoneNumberId}/messages")
            {
                Headers = { { "Authorization", $"Bearer {_configuration["META_TOKEN"]}" } },
                Content = new StringContent(JsonSerializer.Serialize(responsePayload), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                return BadRequest(new { error = "Error sending message", details = responseData });
            }
            return Ok(new { success = "Message sent" });
        }

        return Ok(new { success = "Webhook received!" });
    }
}

public class WhatsAppRequest
{
    public string MetaToken { get; set; }
    public string BusinessPhoneNumberId { get; set; }
    public string EndUserNumber { get; set; }
    public string Message { get; set; }
}