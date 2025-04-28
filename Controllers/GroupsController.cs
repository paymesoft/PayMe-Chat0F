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
    public class GroupsController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        [HttpPost]
        public IActionResult CrearGrupo([FromBody] GroupRequestDto grupo)
        {
            if (string.IsNullOrWhiteSpace(grupo.Name) || grupo.ContactIds.Count == 0)
                return BadRequest(new { error = "El nombre del grupo y al menos un contacto son obligatorios." });

            try
            {
                // Crear DataTable con los IDs
                var table = new DataTable();
                table.Columns.Add("ContactId", typeof(int));
                foreach (var id in grupo.ContactIds)
                {
                    table.Rows.Add(id);
                }

                _dbConnection.Execute(
                    "sp_CreateGroupWithContacts",
                    new
                    {
                        GroupName = grupo.Name,
                        ContactIds = table.AsTableValuedParameter("dbo.IntList")
                    },
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new { message = "Grupo creado correctamente con sus contactos asociados." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al crear el grupo", details = ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> ObtenerGrupos()
        {
            try
            {
                var grupos = await _dbConnection.QueryAsync<GroupResponseDto>(
                    "SELECT Id, Name, CreatedAt FROM ContactGroups ORDER BY CreatedAt DESC"
                );

                return Ok(grupos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener los grupos", details = ex.Message });
            }
        }
    }
}
