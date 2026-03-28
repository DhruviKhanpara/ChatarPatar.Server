using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class UserController : ControllerBase
{
    private readonly IServiceManager _services;

    public UserController(IServiceManager services)
    {
        _services = services;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<AuthUserDto>> GetMe()
    {
        var user = await _services.UserService.GetCurrentUserAsync();
        return Ok(user);
    }
}
