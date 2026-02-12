using Asp.Versioning;
using Authorization_authentication;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Controllers;

[ApiController]
[ApiVersionNeutral]
[Route(WebConstants.ApiControllerRoute)]
public class CommonController : ControllerBase
{
    [HttpGet("health")]
    public IActionResult GetHealth()
    {
        return Ok(new { status = "Healthy", timestamp = DateTime.UtcNow });
    }
}
