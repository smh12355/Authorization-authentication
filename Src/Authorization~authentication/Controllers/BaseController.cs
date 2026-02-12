using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Controllers;

[ApiController]
[Authorize]
[Route("api")]
public class BaseController : ControllerBase
{
    [HttpGet("public")]
    [AllowAnonymous]
    public IActionResult GetPublic()
    {
        return Ok(new { message = "This is a public endpoint, no authentication required!" });
    }

    [HttpGet("protected")]
    public IActionResult GetProtected()
    {
        var username = User.Identity?.Name ?? "Unknown";
        var claims = User.Claims.Select(c => new { c.Type, c.Value });

        return Ok(new
        {
            message = "This is a protected endpoint!",
            username = username,
            claims = claims
        });
    }

    [HttpGet("user")]
    public IActionResult GetUser()
    {
        return Ok(new { message = "Hello User! You have User role." });
    }

    [HttpGet("admin")]
    [Authorize(Roles = "Admin")]
    public IActionResult GetAdmin()
    {
        return Ok(new { message = "Hello Admin! You have Admin role." });
    }
}
