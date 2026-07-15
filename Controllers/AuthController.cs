using Microsoft.AspNetCore.Authentication.Negotiate;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace PlanningTool.NavExtension.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    [HttpGet("windows-login/status")]
    [Authorize(AuthenticationSchemes =
        NegotiateDefaults.AuthenticationScheme)]
    public IActionResult Status()
    {
        return Ok(new
        {
            Authenticated = User.Identity?.IsAuthenticated,
            Username = User.Identity?.Name
        });
    }

    [HttpPost("windows-login")]
    [Authorize(AuthenticationSchemes =
        NegotiateDefaults.AuthenticationScheme)]
    public IActionResult Login()
    {
        return Ok(new
        {
            Username = User.Identity?.Name
        });
    }
}
