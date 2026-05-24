using Microsoft.AspNetCore.Mvc;

namespace TripMindAI.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class HealthController : ControllerBase
{
    [HttpGet]
    public IActionResult Get()
    {
        return Ok(new
        {
            Status = "Healthy",
            Service = "TripMindAI.API",
            Timestamp = DateTime.UtcNow
        });
    }
}
