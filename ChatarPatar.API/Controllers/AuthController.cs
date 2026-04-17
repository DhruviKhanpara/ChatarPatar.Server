using ChatarPatar.API.Attributes;
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
    [SkipPermission]
    public async Task<ActionResult<LoginResponseDto>> Login(UserLoginDto login)
    {
        var authUser = await _services.UserService.LoginUserAsync(login);
        return Ok(authUser);
    }

    [HttpPost("register")]
    [AllowAnonymous]
    [SkipPermission]
    public async Task<ActionResult<LoginResponseDto>> Register(UserRegisterDto user)
    {
        var authUser = await _services.UserService.RegisterUserAsync(user);
        return Ok(authUser);
    }

    [HttpPost("refresh-token")]
    [AllowAnonymous]
    [SkipPermission]
    public async Task<IActionResult> RefreshToken()
    {
        var authUser = await _services.UserService.RefreshAuthToken();
        return Ok(authUser);
    }

    [HttpPost("logout")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> Logout()
    {
        await _services.UserService.LogoutUser();
        return Ok();
    }

    [HttpPost("logout-all-sessions")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> LogoutAllSessions()
    {
        await _services.UserService.LogoutAllUserSessions();
        return Ok("Revoked all session successfully.");
    }

    /// <summary>
    /// Verifies the OTP sent to the user's registered email address.
    /// Marks IsEmailVerified = true on success.
    /// </summary>
    [HttpPost("verify-email")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> VerifyEmail([FromBody] VerifyEmailDto dto)
    {
        await _services.UserService.VerifyEmailAsync(dto);
        return Ok("Email verified successfully.");
    }

    /// <summary>
    /// Resends the email verification OTP.
    /// </summary>
    [HttpPost("resend-verification")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> ResendVerification()
    {
        await _services.UserService.ResendVerificationOtpAsync();
        return Ok("If your email is unverified, a new OTP has been sent.");
    }

    /// <summary>
    /// Step 1 — Request a password-reset OTP.
    /// </summary>
    [HttpPost("forgot-password")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> ForgotPassword(ForgotPasswordDto dto)
    {
        await _services.UserService.ForgotPasswordAsync(dto);
        return Ok("You will receive an OTP shortly on the email.");
    }

    /// <summary>
    /// Step 2 — Verify the OTP and set a new password.
    /// </summary>
    [HttpPost("reset-password")]
    [Authorize]
    [SkipPermission]
    public async Task<IActionResult> ResetPassword(ResetPasswordDto dto)
    {
        await _services.UserService.ResetPasswordAsync(dto);
        return Ok("Password reset successfully. Please login with your new password.");
    }
}
