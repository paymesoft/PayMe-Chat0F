using Microsoft.AspNetCore.Mvc;
using System.Text.Json;
using System.Collections.Generic;

[ApiController]
[Route("api/webhook")]
public class WhatsAppWebhookController : ControllerBase
{
    private static List<string> mensajesRecibidos = new List<string>();
    private const string VerifyToken = "mi_token_secreto"; // Asegúrate de usar este token en Meta

    // 🔹 Endpoint para recibir mensajes desde WhatsApp
    [HttpPost]
    public IActionResult ReceiveMessage([FromBody] JsonElement body)
    {
        Console.WriteLine("📩 Mensaje recibido: " + body.ToString());

        // 🔹 Guardamos el mensaje en memoria en lugar de la base de datos
        mensajesRecibidos.Add(body.ToString());

        return Ok(new { status = "Mensaje recibido" });
    }

    // 🔹 Endpoint para obtener los mensajes almacenados temporalmente
    [HttpGet]
    public IActionResult GetMessages()
    {
        return Ok(mensajesRecibidos);
    }

    // 🔹 Endpoint para la verificación del Webhook con Meta
    [HttpGet]
    public IActionResult VerifyWebhook([FromQuery] string hub_mode, [FromQuery] string hub_challenge, [FromQuery] string hub_verify_token)
    {
        if (hub_mode == "subscribe" && hub_verify_token == VerifyToken)
        {
            return Ok(hub_challenge); // Meta espera recibir esto para validar el Webhook
        }
        return BadRequest("Token incorrecto");
    }
}