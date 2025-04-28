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
    public class ContactsController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        [HttpPost]
        public IActionResult CrearContacto([FromBody] ContactRequestDto contacto)
        {
            if (string.IsNullOrWhiteSpace(contacto.Name) || string.IsNullOrWhiteSpace(contacto.PhoneNumber))
                return BadRequest(new { error = "El nombre y el número de teléfono son obligatorios." });

            try
            {
                _dbConnection.Execute(
                    "sp_InsertContact",
                    new
                    {
                        contacto.Name,
                        contacto.PhoneNumber
                    },
                    commandType: CommandType.StoredProcedure
                );

                return Ok(new { message = "Contacto creado exitosamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al insertar el contacto", details = ex.Message });
            }
        }
    



    [HttpGet]
        public async Task<IActionResult> ObtenerContactos()
        {
            try
            {
                var contactos = await _dbConnection.QueryAsync<ContactResponseDto>(
                    "SELECT Id, Name, PhoneNumber, CreatedAt FROM Contacts ORDER BY CreatedAt DESC"
                );

                return Ok(contactos);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al obtener los contactos", details = ex.Message });
            }
        }

    }


}