using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using PayMeChat_V_1.Models.Entities;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Threading.Tasks;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using MailKit.Security;
using Microsoft.AspNetCore.Http;   // Para usar Response.Cookies
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class AdminPayMeChatLoginController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        /// <summary>
        /// Inicio de sesión del Administrador (con PIN de 5 dígitos).
        /// </summary>
        [HttpPost("login")]
        public async Task<IActionResult> LoginAdministrador([FromBody] AdminLoginDto loginDto)
        {
            // 1) Validaciones básicas
            if (loginDto == null
                || string.IsNullOrWhiteSpace(loginDto.Correo)
                || string.IsNullOrWhiteSpace(loginDto.Contraseña))
            {
                return BadRequest(new { error = "Correo y contraseña son obligatorios." });
            }

            // 2) Buscar admin por correo (SP_ObtenerAdminPorCorreo)
            var parameters = new DynamicParameters();
            parameters.Add("@Correo", loginDto.Correo);

            var admin = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.Sp_ObtenerAdminPorCorreo",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (admin == null)
            {
                return Unauthorized(new { error = "Credenciales incorrectas (usuario no existe)." });
            }

            // 3) Verificar contraseña (usando BCrypt)
            bool contraseñaValida = BCrypt.Net.BCrypt.Verify(
                loginDto.Contraseña,
                (string)admin.ContraseñaHash
            );
            if (!contraseñaValida)
            {
                return Unauthorized(new { error = "Credenciales incorrectas (contraseña inválida)." });
            }

            // 4) Verificar si el correo está marcado como verificado
            if (!(bool)admin.CorreoVerificado)
            {
                return Unauthorized(new { error = "Debe verificar su correo antes de iniciar sesión." });
            }

            // --------------------------------------------
            // Hasta aquí, usuario y contraseña correctos
            // --------------------------------------------

            // 5) Generar un PIN de 5 dígitos
            int codigoAleatorio = new Random().Next(10000, 99999);  // 10000 a 99999
            string codigoPin = codigoAleatorio.ToString();

            // 6) Guardar PIN en la tabla [AdminTokens] con vencimiento a 15 minutos
            var insertarParams = new DynamicParameters();
            insertarParams.Add("@AdminID", (int)admin.Id);
            insertarParams.Add("@Token", codigoPin);
            insertarParams.Add("@Expiracion", DateTime.UtcNow.AddMinutes(15));
            insertarParams.Add("@Estado", true);

            await _dbConnection.ExecuteAsync(
                "dbo.SP_InsertarAdminToken",
                insertarParams,
                commandType: CommandType.StoredProcedure
            );

            // 7) Enviar el PIN por correo
            await EnviarPinPorCorreo((string)admin.Correo, codigoPin);

            // 8) Retornar mensaje de éxito
            return Ok(new
            {
                message = "Inicio de sesión exitoso. Se ha enviado un PIN a tu correo (válido por 15 minutos)."
            });
        }

        /// <summary>
        /// Envía el PIN por correo electrónico.
        /// </summary>
        private async Task EnviarPinPorCorreo(string correoDestino, string pin)
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
                throw new ArgumentException("El correo no puede ser nulo o vacío.");

            var smtpServer = _configuration["SmtpSettings:Server"];
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = _configuration["SmtpSettings:Password"];
            var smtpPort = int.TryParse(_configuration["SmtpSettings:Port"], out int port) ? port : 587;

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PayMeChat Soporte", smtpUsername));
            message.To.Add(new MailboxAddress("", correoDestino));
            message.Subject = "Tu PIN de acceso (válido por 15 minutos)";
            message.Body = new TextPart("plain")
            {
                Text = $"Hola,\n\nTu PIN de acceso es: {pin}\n" +
                       "Este PIN vence en 15 minutos.\n\n" +
                       "Si no solicitaste este acceso, ignora este mensaje."
            };

            using var smtpClient = new SmtpClient();
            await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
            await smtpClient.AuthenticateAsync(smtpUsername, smtpPassword);
            await smtpClient.SendAsync(message);
            await smtpClient.DisconnectAsync(true);
        }

        /// <summary>
        /// Verifica el PIN que el usuario ingresa (reutilizando tu SP_VerificarAdminToken).
        ///  - 0 => No existe
        ///  - 2 => Expirado/Usado
        ///  - 1 => Válido
        /// </summary>
        [HttpGet("VerifyLogin")]
        public async Task<IActionResult> VerifyLogin([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { status = "error", message = "No se proporcionó el token (PIN)." });
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Token", token);

            // Llamamos SP_VerificarAdminToken
            int resultado = await _dbConnection.ExecuteScalarAsync<int>(
                "dbo.SP_VerificarAdminToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // Interpretamos el resultado
            string status;
            string message;

            switch (resultado)
            {
                case 1:
                    status = "success";
                    message = "PIN válido. Acceso verificado.";
                    break;
                case 2:
                    status = "warning";
                    message = "PIN expirado o ya utilizado.";
                    break;
                default:
                    status = "error";
                    message = "PIN no válido o no encontrado.";
                    break;
            }

            return Ok(new { status, message });
        }


    } // Fin de la clase
}
