using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using PayMeChat_V_1.Models.Entities;
using MimeKit;
using MailKit.Net.Smtp;
using System;
using System.Threading.Tasks;
using MailKit.Security;

// Asegúrate de tener estos usings
using Microsoft.AspNetCore.Http; // Para usar Response.Cookies
using Microsoft.AspNetCore.Hosting; // Para IWebHostEnvironment

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]  // Asegura que la API responde en formato JSON
    public class AdminPayMeChatController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        /// <summary>
        /// Registro de Administrador con verificación de correo
        /// </summary>
        [HttpPost("register")]
        public async Task<IActionResult> RegistrarAdministrador([FromBody] AdminRegisterDto adminDto)
        {
            if (adminDto == null || string.IsNullOrWhiteSpace(adminDto.NombreUsuario) ||
                string.IsNullOrWhiteSpace(adminDto.Contraseña) || string.IsNullOrWhiteSpace(adminDto.Correo))
            {
                return BadRequest(new { error = "Todos los campos son obligatorios." });
            }

            var existingUser = await _dbConnection.QueryFirstOrDefaultAsync<dynamic>(
                "SELECT (SELECT COUNT(1) FROM dbo.AdminPayMeChat WHERE NombreUsuario = @NombreUsuario) AS ExisteNombre, " +
                "(SELECT COUNT(1) FROM dbo.AdminPayMeChat WHERE Correo = @Correo) AS ExisteCorreo",
                new { NombreUsuario = adminDto.NombreUsuario, Correo = adminDto.Correo }
            );

            if (existingUser.ExisteNombre > 0)
            {
                return Conflict(new { error = "El nombre de usuario ya está registrado." });
            }

            if (existingUser.ExisteCorreo > 0)
            {
                return Conflict(new { error = "El correo ya está registrado." });
            }

            string contraseñaHasheada = BCrypt.Net.BCrypt.HashPassword(adminDto.Contraseña);
            string tokenVerificacion = Guid.NewGuid().ToString();

            var parameters = new DynamicParameters();
            parameters.Add("@NombreUsuario", adminDto.NombreUsuario);
            parameters.Add("@ContraseñaHash", contraseñaHasheada);
            parameters.Add("@Correo", adminDto.Correo);
            parameters.Add("@Activo", false);
            parameters.Add("@CorreoVerificado", false);
            parameters.Add("@TokenVerificacion", tokenVerificacion);
            parameters.Add("@UsuarioID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            await _dbConnection.ExecuteAsync(
                "dbo.Sp_RegistrarAdminPayMeChat",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            int usuarioId = parameters.Get<int>("@UsuarioID");
            if (usuarioId == 0)
            {
                return StatusCode(500, new { error = "No se pudo registrar el administrador." });
            }

            string baseFrontendUrl = _configuration["AppSettings:BaseFrontendUrl"] ?? $"{Request.Scheme}://{Request.Host.Value}";
            string enlaceVerificacion = $"{baseFrontendUrl}/verify?correo={Uri.EscapeDataString(adminDto.Correo)}&token={Uri.EscapeDataString(tokenVerificacion)}";

            await EnviarCorreoVerificacion(adminDto.Correo, enlaceVerificacion);

            return Ok(new { message = "Administrador registrado correctamente. Verifique su correo." });
        }

        /// <summary>
        /// Enviar verificación de correo con enlace único
        /// </summary>
        private async Task EnviarCorreoVerificacion(string correoDestino, string enlaceVerificacion)
        {
            if (string.IsNullOrWhiteSpace(correoDestino))
            {
                throw new ArgumentException("El correo no puede ser nulo o vacío.");
            }

            var smtpServer = _configuration["SmtpSettings:Server"];
            var smtpPort = int.TryParse(_configuration["SmtpSettings:Port"], out int port) ? port : 587;
            var smtpUsername = _configuration["SmtpSettings:Username"];
            var smtpPassword = Environment.GetEnvironmentVariable("SMTP_MailKit_2");

            if (string.IsNullOrWhiteSpace(smtpUsername) || string.IsNullOrWhiteSpace(smtpPassword))
            {
                throw new Exception("Error: Las credenciales SMTP no están configuradas correctamente.");
            }

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress("PayMeChat Soporte", smtpUsername));
            message.To.Add(new MailboxAddress("", correoDestino));
            message.Subject = "Verificación de Cuenta - PayMeChat";
            message.Body = new TextPart("plain")
            {
                Text = $"Haga clic en el siguiente enlace para verificar su cuenta:\n\n{enlaceVerificacion}"
            };

            try
            {
                using (var smtpClient = new SmtpClient())
                {
                    smtpClient.ServerCertificateValidationCallback = (s, c, h, e) => true;
                    await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                    await smtpClient.AuthenticateAsync(smtpUsername, smtpPassword);
                    await smtpClient.SendAsync(message);
                    await smtpClient.DisconnectAsync(true);
                }
            }
            catch (MailKit.Security.AuthenticationException)
            {
                throw new Exception("Error de autenticación en el servidor SMTP. Verifique usuario y contraseña.");
            }
            catch (SmtpCommandException)
            {
                throw new Exception("Error en la conexión SMTP. Puede ser un problema con las credenciales o el puerto.");
            }
            catch (Exception ex)
            {
                throw new Exception($"Error inesperado al enviar el correo: {ex.Message}");
            }
        }

        /// <summary>
        /// Verificar correo (GET)
        /// </summary>
        [HttpGet("verificar")]
        public async Task<IActionResult> VerificarCorreo([FromQuery] string correo, [FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(correo) || string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { status = "error", message = "Correo o token inválido" });
            }

            var parameters = new DynamicParameters();
            parameters.Add("@Correo", correo);
            parameters.Add("@TokenVerificacion", token);

            int? resultado = await _dbConnection.ExecuteScalarAsync<int?>(
                "dbo.Sp_VerificarAdminPayMeChat",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            string status;
            string message;

            if (resultado == 1)
            {
                var tokenParams = new DynamicParameters();
                tokenParams.Add("@Correo", correo);

                await _dbConnection.ExecuteAsync(
                    "dbo.Sp_E_TokenVerificacion_Chat",
                    tokenParams,
                    commandType: CommandType.StoredProcedure
                );

                status = "success";
                message = "Cuenta verificada correctamente.";
            }
            else if (resultado == 2)
            {
                status = "warning";
                message = "La cuenta ya fue verificada anteriormente.";
            }
            else
            {
                status = "error";
                message = "Token inválido o expirado.";
            }

            return Ok(new { status, message });
        }
    }
}
