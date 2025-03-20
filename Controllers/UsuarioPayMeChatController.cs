using Dapper;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.SqlClient;
using PayMeChat.V1.Backend.Entities;
using PayMeChat_V_1.Models.Entities;
using System.Data;

[Route("api/[controller]")]
[ApiController]
public class UsuarioPayMeChatController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly IDbConnection _dbConnection;
    private readonly string _connectionString;

    public UsuarioPayMeChatController(IConfiguration configuration, IDbConnection dbConnection)
    {
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _dbConnection = dbConnection ?? throw new ArgumentNullException(nameof(dbConnection));
        _connectionString = _configuration.GetConnectionString("DefaultConnection")
                            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
    }

    [HttpPost("register")]
    public async Task<IActionResult> RegistrarUsuario([FromBody] UsuarioPayMeChat usuario)
    {
        try
        {
            using var connection = new SqlConnection(_connectionString);

            // 🔒 Hashear la contraseña antes de enviarla al SP
            string contraseñaHasheada = BCrypt.Net.BCrypt.HashPassword(usuario.ContraseñaHash);

            var parameters = new DynamicParameters();
            parameters.Add("@NombreUsuario", usuario.NombreUsuario);
            parameters.Add("@Correo", usuario.Correo);
            parameters.Add("@ContraseñaHash", contraseñaHasheada);
            parameters.Add("@ServidorURL", usuario.ServidorURL);
            parameters.Add("@Telefono", usuario.Telefono);
            parameters.Add("@Activo", usuario.Activo);
            parameters.Add("@UsuarioID", dbType: DbType.Int32, direction: ParameterDirection.Output);

            // 🔹 Ejecutar el SP
            await connection.ExecuteAsync("dbo.Sp_RegistrarUsuarioPayMeChat", parameters, commandType: CommandType.StoredProcedure);

            // 🔹 Obtener el ID generado
            int usuarioId = parameters.Get<int>("@UsuarioID");

            if (usuarioId == 0)
            {
                return StatusCode(500, new { error = "No se pudo obtener el ID del usuario registrado." });
            }

            return Ok(new
            {
                message = "Usuario registrado correctamente.",
                usuarioId,
                usuario.NombreUsuario,
                usuario.Correo
            });
        }
        catch (SqlException ex)
        {
            if (ex.Number == 50001) // Código de error definido en el SP para usuario duplicado
            {
                return Conflict(new { error = "El usuario ya existe en el sistema." });
            }
            return StatusCode(500, new { error = $"Error SQL: {ex.Message}" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { error = $"Error general: {ex.Message}" });
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

        int resultado = await connection.ExecuteScalarAsync<int>(
            "dbo.SP_VerificarUsuarioToken",
            parameters,
            commandType: CommandType.StoredProcedure
        );

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

}


