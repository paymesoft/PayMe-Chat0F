using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;
using MailKit.Net.Smtp;
using MailKit.Security;
using System;
using System.Threading.Tasks;

[Route("api/[controller]")]
[ApiController]
public class EmailTestController : ControllerBase
{
    private readonly IConfiguration _config;
    private readonly ILogger<EmailTestController> _logger;

    public EmailTestController(IConfiguration config, ILogger<EmailTestController> logger)
    {
        _config = config ?? throw new ArgumentNullException(nameof(config));
        _logger = logger;
    }

    [HttpGet("test-email")]
    public async Task<IActionResult> TestEmail()
    {
        string testEmail = "nathalie.arroyavez@gmail.com"; // Cambia esto por tu correo de prueba
        _logger.LogInformation("📧 Enviando correo de prueba a: {0}", testEmail);

        await EnviarCorreoPrueba("Prueba", testEmail, "12345");

        return Ok(new { message = $"Correo de prueba enviado a {testEmail}" });
    }

    private async Task EnviarCorreoPrueba(string nombre, string correoDestino, string pin)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                _logger.LogError("❌ ERROR: El correo del usuario está vacío o nulo.");
                return;
            }

            var smtpServer = _config["SmtpSettings:Server"];
            var smtpUsername = _config["SmtpSettings:Username"];
            var smtpPassword = _config["SmtpSettings:Password"];
            var smtpPort = int.TryParse(_config["SmtpSettings:Port"], out int port) ? port : 587;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PayMeChat Soporte", smtpUsername));
            message.To.Add(new MailboxAddress(nombre, correoDestino));
            message.Subject = "Correo de prueba";
            message.Body = new TextPart("plain")
            {
                Text = $"Hola {nombre},\n\nEste es un correo de prueba.\nPIN: {pin}\n\nSaludos."
            };

            using var smtpClient = new SmtpClient();
            _logger.LogInformation("📧 Conectando a SMTP: {0}:{1}", smtpServer, smtpPort);

            await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            _logger.LogInformation("✅ Conexión SMTP establecida.");

            await smtpClient.AuthenticateAsync(smtpUsername, smtpPassword);
            _logger.LogInformation("✅ Autenticación SMTP correcta.");

            await smtpClient.SendAsync(message);
            _logger.LogInformation("✅ Correo enviado con éxito a {0}", correoDestino);

            await smtpClient.DisconnectAsync(true);
        }
        catch (Exception ex)
        {
            _logger.LogError("❌ Error al enviar correo: {0}", ex.Message);
        }
    }
}
