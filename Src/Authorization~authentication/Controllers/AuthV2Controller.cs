using Asp.Versioning;
using Authorization_authentication.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Controllers;

[ApiController]
[Authorize]
[ApiVersion("2.0")]
[Route(WebConstants.ApiControllerRoute)]
public class AuthV2Controller : ControllerBase
{
    // Пример нового эндпоинта только для v2
    [HttpGet("protected")]
    public IActionResult GetProtectedV2()
    {
        var username = User.Identity?.Name ?? "Unknown";

        return Ok(new
        {
            message = "This is a PROTECTED endpoint from API v2!",
            username,
            version = "2.0"
        });
    }

    // Отдельный эндпоинт, чтобы было видно разницу между версиями
    [HttpGet("info")]
    [AllowAnonymous]
    public IActionResult GetInfo()
    {
        return Ok(new
        {
            message = "This is API v2 endpoint.",
            version = "2.0",
            note = "You are calling the second version of the Auth API."
        });
    }
}
