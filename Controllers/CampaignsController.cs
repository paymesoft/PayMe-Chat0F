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
    public class CampaignsController(IConfiguration configuration, IDbConnection dbConnection) : ControllerBase
    {
        private readonly IConfiguration _configuration = configuration;
        private readonly IDbConnection _dbConnection = dbConnection;

        [HttpPost("send")]
        public async Task<IActionResult> EnviarCampaña([FromBody] CampaignRequestDto campaña)
        {
            if (campaña.GroupId <= 0 || campaña.TemplateId <= 0)
                return BadRequest(new { error = "El ID del grupo y el ID de la plantilla son obligatorios." });

            try
            {
                // Obtener contenido de la plantilla
                var plantilla = await _dbConnection.QueryFirstOrDefaultAsync<string>(
                    "SELECT Content FROM Templates WHERE Id = @Id",
                    new { Id = campaña.TemplateId }
                );

                if (string.IsNullOrEmpty(plantilla))
                    return NotFound(new { error = "Plantilla no encontrada." });

                // Obtener los números del grupo
                var numeros = await _dbConnection.QueryAsync<string>(
                    @"SELECT c.PhoneNumber
                      FROM GroupContacts gc
                      INNER JOIN Contacts c ON gc.ContactId = c.Id
                      WHERE gc.GroupId = @GroupId",
                    new { GroupId = campaña.GroupId }
                );

                if (!numeros.Any())
                    return NotFound(new { error = "El grupo no tiene contactos asociados." });

                // Enviar mensajes a cada número
                using var httpClient = new HttpClient();
                foreach (var numero in numeros)
                {
                    var formData = new MultipartFormDataContent
                    {
                        { new StringContent(_configuration["PHONE_NUMBER_ID"]), "PhoneNumberId" },
                        { new StringContent(numero), "EndUserNumber" },
                        { new StringContent(plantilla), "Message" },
                        { new StringContent(_configuration["META_TOKEN"]), "MetaToken" }
                    };

                    await httpClient.PostAsync("http://localhost:5198/api/whatsapp/send-message", formData);
                }

                // Registrar campaña
                await _dbConnection.ExecuteAsync(
                    "sp_InsertCampaign",
                    new { campaña.GroupId, campaña.TemplateId }
                );

                return Ok(new { message = "Campaña enviada correctamente." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { error = "Error al enviar la campaña", details = ex.Message });
            }
        }
    }
}
