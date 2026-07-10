using Microsoft.AspNetCore.Mvc;

namespace Safarni.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            status = "Healthy",
            service = "Safarni.API",
            timestamp = DateTime.UtcNow
        });
    }
}
