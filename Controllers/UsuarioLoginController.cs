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
using Microsoft.AspNetCore.Http;
using Microsoft.Data.SqlClient;
using System.Data.Common;

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class UsuarioLoginController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly string _connectionString;

        public UsuarioLoginController(IConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _connectionString = _config.GetConnectionString("DefaultConnection")
                                ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
        }

        [HttpPost("login")]
        public async Task<IActionResult> LoginUsuario([FromBody] UsuarioLoginDto loginDto)
        {
            Console.WriteLine($"🔹 Intento de inicio de sesión para usuario: {loginDto.NombreUsuario}");

            if (loginDto == null || string.IsNullOrWhiteSpace(loginDto.NombreUsuario) || string.IsNullOrWhiteSpace(loginDto.Contraseña))
            {
                Console.WriteLine("❌ Error: Usuario y contraseña son obligatorios.");
                return BadRequest(new { error = "Usuario y contraseña son obligatorios." });
            }

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@NombreUsuario", loginDto.NombreUsuario);

            var usuario = await connection.QueryFirstOrDefaultAsync<dynamic>(
                "dbo.Sp_ObtenerUsuarioPorNombre",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            if (usuario == null)
            {
                Console.WriteLine("❌ Error: Usuario no encontrado.");
                return Unauthorized(new { error = "Credenciales incorrectas (usuario no existe)." });
            }

            Console.WriteLine("✅ Usuario encontrado en la base de datos.");

            bool contraseñaValida = BCrypt.Net.BCrypt.Verify(
                loginDto.Contraseña,
                (string)usuario.ContraseñaHash
            );
            if (!contraseñaValida)
            {
                Console.WriteLine("❌ Error: Contraseña incorrecta.");
                return Unauthorized(new { error = "Credenciales incorrectas (contraseña inválida)." });
            }

            if (!(bool)usuario.Activo)
            {
                Console.WriteLine("❌ Error: Cuenta inactiva.");
                return Unauthorized(new { error = "Debe activar su cuenta antes de iniciar sesión." });
            }

            Console.WriteLine("✅ Usuario autenticado correctamente.");

            // 🔹 Conversión a un objeto tipado para evitar problemas con `dynamic`
            var usuarioObj = new
            {
                Id = (int)usuario.Id,
                NombreUsuario = (string)usuario.NombreUsuario,
                Correo = usuario.Correo != null ? (string)usuario.Correo : string.Empty
            };

            // 🔹 Verificar que el correo no sea null o vacío
            if (string.IsNullOrWhiteSpace(usuarioObj.Correo))
            {
                Console.WriteLine("❌ ERROR: El usuario no tiene un correo registrado.");
                return StatusCode(500, new { error = "No se puede enviar el PIN porque el usuario no tiene un correo registrado." });
            }

            Console.WriteLine($"📧 Correo obtenido de la BD: {usuarioObj.Correo}");

            // 🔹 Generar PIN
            int codigoAleatorio = new Random().Next(10000, 99999);
            string codigoPin = codigoAleatorio.ToString();

            var insertarParams = new DynamicParameters();
            insertarParams.Add("@UsuarioID", usuarioObj.Id);
            insertarParams.Add("@Token", codigoPin);
            insertarParams.Add("@Expiracion", DateTime.UtcNow.AddMinutes(15));
            insertarParams.Add("@Estado", true);

            await connection.ExecuteAsync(
                "dbo.SP_InsertarUsuarioToken",
                insertarParams,
                commandType: CommandType.StoredProcedure
            );

            Console.WriteLine($"📧 Enviando PIN al correo: {usuarioObj.Correo}");
            await EnviarPinPorCorreo(usuarioObj.NombreUsuario, usuarioObj.Correo, codigoPin);

            return Ok(new
            {
                message = "Inicio de sesión exitoso. Se ha enviado un PIN a tu correo (válido por 15 minutos)."
            });
        }

        private async Task EnviarPinPorCorreo(string nombre, string correoDestino, string pin)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(correoDestino))
                {
                    Console.WriteLine("❌ ERROR: El correo del usuario está vacío o nulo.");
                    return;
                }

                var smtpServer = _config["SmtpSettings:Server"];
                var smtpUsername = _config["SmtpSettings:Username"];
                var smtpPassword = _config["SmtpSettings:Password"];
                var smtpPort = int.TryParse(_config["SmtpSettings:Port"], out int port) ? port : 587;

                Console.WriteLine($"📧 Conectando a SMTP {smtpServer}:{smtpPort}");

                var message = new MimeMessage();
                message.From.Add(new MailboxAddress("PayMeChat Soporte", smtpUsername));
                message.To.Add(new MailboxAddress(nombre, correoDestino));
                message.Subject = "Tu PIN de acceso (válido por 15 minutos)";
                message.Body = new TextPart("plain")
                {
                    Text = $"Hola {nombre},\n\nTu PIN de acceso es: {pin}\n" +
                           "Este PIN vence en 15 minutos.\n\n" +
                           "Si no solicitaste este acceso, ignora este mensaje."
                };

                using var smtpClient = new SmtpClient();
                await smtpClient.ConnectAsync(smtpServer, smtpPort, SecureSocketOptions.StartTls);
                await smtpClient.AuthenticateAsync(smtpUsername, smtpPassword);
                await smtpClient.SendAsync(message);
                await smtpClient.DisconnectAsync(true);

                Console.WriteLine("✅ Correo enviado con éxito.");
            }
            catch (Exception ex)
            {
                Console.WriteLine("❌ Error al enviar correo: " + ex.Message);
            }
        }

        [HttpGet("VerifyLogin")]
        public async Task<IActionResult> VerifyLogin([FromQuery] string token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return BadRequest(new { status = "error", message = "No se proporcionó el token (PIN)." });
            }

            using var connection = new SqlConnection(_connectionString);
            var parameters = new DynamicParameters();
            parameters.Add("@Token", token);

            // 🔹 Obtener el resultado del SP
            int resultado = await connection.QuerySingleOrDefaultAsync<int>(
                "dbo.SP_VerificarUsuarioToken",
                parameters,
                commandType: CommandType.StoredProcedure
            );

            // 🔹 Interpretar el resultado correctamente
            switch (resultado)
            {
                case 1:
                    return Ok(new { status = "success", message = "PIN válido. Acceso verificado." });
                case 2:
                    return BadRequest(new { status = "warning", message = "PIN expirado o ya utilizado." });
                default:
                    return BadRequest(new { status = "error", message = "PIN no válido o no encontrado." });
            }
        }


    }
}