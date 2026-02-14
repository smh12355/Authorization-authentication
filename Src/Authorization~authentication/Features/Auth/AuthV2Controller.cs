using Asp.Versioning;
using Authorization_authentication.Common.Constants;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Authorization_authentication.Features.Auth;

[ApiController]
[Authorize]
[ApiVersion("2.0")]
[Route(WebConstants.ApiControllerRoute)]
public class AuthV2Controller : ControllerBase
{
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
