// WhatsAppController.cs - Versión estable anterior

using Microsoft.AspNetCore.Mvc;
using System.Net.Http;
using System.Text.Json;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using PayMeChat_V_1.Models.Entities;
using System;
using Microsoft.Data.SqlClient;
using Dapper;
using System.Collections.Generic;

namespace YourNamespaceHere
{
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
        public async Task<IActionResult> SendMessage([FromForm] WhatsAppRequestDto request)
        {
            string metaToken = request.MetaToken ?? _configuration["META_TOKEN"] ?? string.Empty;
            if (string.IsNullOrEmpty(metaToken))
                return BadRequest(new { error = "Missing META token" });

            var payload = new
            {
                messaging_product = "whatsapp",
                to = request.EndUserNumber,
                text = new { body = request.Message }
            };

            var httpRequest = new HttpRequestMessage(
                HttpMethod.Post,
                $"https://graph.facebook.com/v22.0/{request.PhoneNumberId}/messages")
            {
                Headers = { { "Authorization", $"Bearer {metaToken}" } },
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest);
            var responseData = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
                return BadRequest(new { error = "Failed to send message", details = responseData });

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            await conn.ExecuteAsync("INSERT INTO WhatsAppMessages (PhoneNumber, Direction, Content, Timestamp) VALUES (@to, 'outbound', @body, GETUTCDATE())",
                new { to = request.EndUserNumber, body = request.Message });

            return Ok(new { success = "Message sent successfully!" });
        }

        [HttpGet("webhook")]
        public IActionResult VerifyWebhook(
            [FromQuery(Name = "hub.mode")] string hubMode,
            [FromQuery(Name = "hub.verify_token")] string hubVerifyToken,
            [FromQuery(Name = "hub.challenge")] string hubChallenge)
        {
            if (hubMode == "subscribe" && hubVerifyToken == "abc123")
                return Content(hubChallenge, "text/plain");

            return Forbid();
        }

        [HttpPost("webhook")]
        public async Task<IActionResult> ReceiveWebhook([FromBody] JsonDocument body)
        {
            try
            {
                var entry = body.RootElement.GetProperty("entry")[0]
                                     .GetProperty("changes")[0]
                                     .GetProperty("value");

                var message = entry.GetProperty("messages")[0];
                var phoneNumberId = entry.GetProperty("metadata").GetProperty("phone_number_id").GetString();

                if (message.GetProperty("type").GetString() == "text")
                {
                    var from = message.GetProperty("from").GetString();
                    var content = message.GetProperty("text").GetProperty("body").GetString();

                    using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
                    await conn.ExecuteAsync("INSERT INTO WhatsAppMessages (PhoneNumber, Direction, Content, Timestamp) VALUES (@from, 'inbound', @content, GETUTCDATE())",
                        new { from, content });

                    return Ok();
                }
            }
            catch (Exception ex)
            {
                return BadRequest(new { error = ex.Message });
            }

            return Ok();
        }

        [HttpGet("messages/{number}")]
        public async Task<IActionResult> GetMessages(string number)
        {
            var sql = "SELECT PhoneNumber, Direction, Content, Timestamp FROM WhatsAppMessages WHERE PhoneNumber = @Number ORDER BY Timestamp ASC";

            using var conn = new SqlConnection(_configuration.GetConnectionString("DefaultConnection"));
            var messages = await conn.QueryAsync(sql, new { Number = number });

            return Ok(messages);
        }
    }
}