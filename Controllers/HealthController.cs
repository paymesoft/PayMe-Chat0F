using Microsoft.AspNetCore.Mvc;

namespace PayMeChat_V_1.Controllers
{
    [ApiController]
    [Route("api/health")]
    public class HealthController : ControllerBase
    {
        [HttpGet]
        public IActionResult GetHealth()
        {
            return Ok(new { status = "API corriendo correctamente" });
        }
    }
}
