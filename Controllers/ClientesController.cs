using Microsoft.AspNetCore.Mvc;
using System.Data;
using Dapper;
using Microsoft.Extensions.Configuration;
using PayMeChat_V_1.Models.Entities;

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ClientesController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        // Crear un nuevo cliente usando el SP
        [HttpPost]
        public async Task<IActionResult> CrearCliente([FromBody] ClientesAdmin cliente)
        {
            try
            {
                if (cliente == null)
                {
                    return BadRequest(new { message = "Datos de cliente no válidos." });
                }

                if (string.IsNullOrEmpty(cliente.NombreCliente) || string.IsNullOrEmpty(cliente.Correo))
                {
                    return BadRequest(new { message = "Nombre y Correo son obligatorios." });
                }

                // ⚠️ No enviar `id` ni `FechaCreacion` si la BD ya lo maneja
                cliente.Activo = true;

                var parametros = new
                {
                    cliente.NombreCliente,
                    cliente.NombreRepre,
                    cliente.Correo,
                    cliente.Telefono,
                    cliente.DireccionEmpresa,
                    cliente.Activo
                };

                int filasAfectadas = await _dbConnection.ExecuteAsync(
                    "sp_CrearCliente", parametros, commandType: CommandType.StoredProcedure
                );

                return filasAfectadas > 0
                    ? Ok(new { message = "Cliente creado correctamente" })
                    : BadRequest(new { message = "Error al crear el cliente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error interno al crear cliente", error = ex.Message });
            }
        }


        // Obtener la lista de clientes usando el SP
        [HttpGet]
        public async Task<IActionResult> ObtenerClientes()
        {
            try
            {
                var clientes = await _dbConnection.QueryAsync<ClientesAdmin>("sp_ObtenerClientes", commandType: CommandType.StoredProcedure);

                // ⚠️ Asegurar que se devuelva un array, incluso si no hay clientes
                return Ok(clientes ?? new List<ClientesAdmin>());
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener clientes", error = ex.Message });
            }
        }

        // Obtener un cliente por ID usando el SP
        [HttpGet("{id}")]
        public async Task<IActionResult> ObtenerClientePorId(int id)
        {
            try
            {
                var cliente = await _dbConnection.QueryFirstOrDefaultAsync<ClientesAdmin>(
                    "sp_ObtenerClientePorId",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );

                return cliente != null ? Ok(cliente) : NotFound(new { message = "Cliente no encontrado" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al obtener cliente", error = ex.Message });
            }
        }

        // Editar un cliente usando el SP
        [HttpPut("{id}")]
        public async Task<IActionResult> EditarCliente(int id, [FromBody] ClientesAdmin cliente)
        {
            try
            {
                var parametros = new
                {
                    Id = id,
                    cliente.NombreCliente,
                    cliente.NombreRepre,
                    cliente.Correo,
                    cliente.Telefono,
                    cliente.DireccionEmpresa,
                    cliente.Activo
                };

                int filasAfectadas = await _dbConnection.ExecuteAsync("sp_EditarCliente", parametros, commandType: CommandType.StoredProcedure);

                return filasAfectadas > 0 ? Ok(new { message = "Cliente actualizado correctamente" }) :
                                            BadRequest(new { message = "Error al actualizar el cliente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al editar cliente", error = ex.Message });
            }
        }

        // Eliminar un cliente usando el SP
        [HttpDelete("{id}")]
        public async Task<IActionResult> EliminarCliente(int id)
        {
            try
            {
                int filasAfectadas = await _dbConnection.ExecuteAsync("sp_EliminarCliente", new { Id = id }, commandType: CommandType.StoredProcedure);

                return filasAfectadas > 0 ? Ok(new { message = "Cliente eliminado correctamente" }) :
                                            BadRequest(new { message = "Error al eliminar el cliente" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { message = "Error al eliminar cliente", error = ex.Message });
            }
        }
    }
}
