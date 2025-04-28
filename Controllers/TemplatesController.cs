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
    public class TemplatesController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        [HttpPost]
        public IActionResult CrearPlantilla([FromBody] TemplateRequestDto plantilla)
        {
            if (string.IsNullOrWhiteSpace(plantilla.Name) || string.IsNullOrWhiteSpace(plantilla.Content))
                return BadRequest(new { error = "El nombre y el contenido de la plantilla son obligatorios." });

            try
            {
                _dbConnection.Execute(
                    "sp_InsertTemplate",
                    new
                    {
                        plantilla.Name,
                        plantilla.Content
                    },
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new { message = "Plantilla creada exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al insertar la plantilla", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerPlantillas()
        {
            try
            {
                var plantillas = await _dbConnection.QueryAsync<TemplateResponseDto>(
                    "SELECT Id, Name, Content, CreatedAt FROM Templates ORDER BY CreatedAt DESC"
                );

                return Ok(plantillas);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener las plantillas", details = ex.Message });
            }
        }

        [HttpPut("{id}")]
        public IActionResult ActualizarPlantilla(int id, [FromBody] TemplateRequestDto plantilla)
        {
            if (string.IsNullOrWhiteSpace(plantilla.Name) || string.IsNullOrWhiteSpace(plantilla.Content))
                return BadRequest(new { error = "El nombre y contenido son obligatorios." });

            try
            {
                var filasAfectadas = _dbConnection.Execute(
                    "sp_UpdateTemplate",
                    new
                    {
                        Id = id,
                        plantilla.Name,
                        plantilla.Content
                    },
                    commandType: CommandType.StoredProcedure
                );

                if (filasAfectadas == 0)
                    return NotFound(new { error = "Plantilla no encontrada." });

                return Ok(new { message = "Plantilla actualizada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al actualizar la plantilla", details = ex.Message });
            }
        }

        [HttpDelete("{id}")]
        public IActionResult EliminarPlantilla(int id)
        {
            try
            {
                var filasAfectadas = _dbConnection.Execute(
                    "sp_DeleteTemplate",
                    new { Id = id },
                    commandType: CommandType.StoredProcedure
                );

                if (filasAfectadas == 0)
                    return NotFound(new { error = "Plantilla no encontrada." });

                return Ok(new { message = "Plantilla eliminada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al eliminar la plantilla", details = ex.Message });
            }
        }
    }
}
