using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using System.IO;
using System.Text.Json;
using System;

namespace PayMeChat_V1_Backend.Controllers
{
    [ApiController]
    [Route("api/webhook")]
    public class WebhookController : ControllerBase
    {
        private readonly ILogger<WebhookController> _logger;

        public WebhookController(ILogger<WebhookController> logger)
        {
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> ReceiveWebhook()
        {
            try
            {
                using var reader = new StreamReader(Request.Body);
                var body = await reader.ReadToEndAsync();

                _logger.LogInformation("Webhook recibido: {0}", body);

                // Puedes deserializar si el JSON tiene una estructura específica
                var jsonData = JsonSerializer.Deserialize<object>(body);

                return Ok(new { success = true, message = "Webhook recibido correctamente" });
            }
            catch (Exception ex)
            {
                _logger.LogError("Error procesando el webhook: {0}", ex.Message);
                return BadRequest(new { success = false, message = "Error procesando el webhook" });
            }
        }
    }
}
