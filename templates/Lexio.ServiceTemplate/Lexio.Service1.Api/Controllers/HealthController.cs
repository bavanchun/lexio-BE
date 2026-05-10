using Microsoft.AspNetCore.Mvc;

namespace Lexio.Service1.Api.Controllers;

/// <summary>Smoke-test endpoint — returns 200 to confirm service is running.</summary>
[ApiController]
[Route("[controller]")]
public sealed class HealthController : ControllerBase
{
    [HttpGet("smoke")]
    public IActionResult Smoke() => Ok("Service1 alive");
}
