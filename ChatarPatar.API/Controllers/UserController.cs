using ChatarPatar.Application.DTOs.Common;
using ChatarPatar.Application.DTOs.User;
using ChatarPatar.Application.ServiceContracts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ChatarPatar.API.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class UserController : ControllerBase
{
    private readonly IServiceManager _services;

    public UserController(IServiceManager services)
    {
        _services = services;
    }

    /// <summary>
    /// Returns the lightweight auth identity of the currently logged-in user.
    /// </summary>
    [HttpGet("me")]
    public async Task<ActionResult<AuthUserDto>> GetMe()
    {
        var user = await _services.UserService.GetCurrentUserAsync();
        return Ok(user);
    }

    /// <summary>
    /// Returns the full profile of the currently logged-in user.
    /// </summary>
    [HttpGet("profile/me")]
    public async Task<ActionResult<UserProfileDto>> GetMyProfile()
    {
        var profile = await _services.UserService.GetUserProfileAsync<UserProfileDto>();
        return Ok(profile);
    }

    /// <summary>
    /// Returns the full profile of the provided userId.
    /// </summary>
    [HttpGet("profile/{userId:guid}")]
    public async Task<ActionResult<UserProfileSummaryDto>> GetUserProfile([FromRoute] Guid userId)
    {
        var profile = await _services.UserService.GetUserProfileAsync<UserProfileSummaryDto>(userId: userId);
        return Ok(profile);
    }

    /// <summary>
    /// update user profile of the currently logged-in user.
    /// </summary>
    [HttpPatch("me")]
    public async Task<IActionResult> UpdateProfile([FromBody] UserUpdateDto model)
    {
        await _services.UserService.UpdateUserAsync(model);
        return Ok("User Update successfully");
    }

    /// <summary>
    /// update avatar for the currently logged-in user.
    /// </summary>
    [HttpPatch("me/avatar")]
    public async Task<IActionResult> UpdateMyAvatar([FromForm] ImageUploadDto dto)
    {
        await _services.UserService.UpdateAvatarAsync(dto: dto);
        return Ok("Update avatar successfully.");
    }
}
