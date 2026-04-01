using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IServiceManager _services;

    public AuthController(IServiceManager services)
    {
        _services = services;
    }

    [HttpPost("login")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Login(UserLoginDto login)
    {
        var authUser = await _services.UserService.LoginUserAsync(login);
        return Ok(authUser);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    public async Task<ActionResult<LoginResponseDto>> Register(UserRegisterDto user)
    {
        var authUser = await _services.UserService.RegisterUserAsync(user);
        return Ok(authUser);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    public async Task<IActionResult> RefreshToken()
    {
        var authUser = await _services.UserService.RefreshAuthToken();
        return Ok(authUser);
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<IActionResult> Logout()
    {
        await _services.UserService.LogoutUser();
        return Ok();
    }

    [HttpPost("revoke-all-sessions")]
    [Authorize]
    public async Task<IActionResult> RevokeAllSessions()
    {
        await _services.UserService.RevokeAllUserSessions();
        return Ok("Revoked all session successfully.");
    }
}
